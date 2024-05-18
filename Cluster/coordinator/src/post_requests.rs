
use std::sync::{Arc, Weak};

use actix_web::{web, HttpResponse, Responder};

use crate::{communication::NodeRegisterResponse, state::{InteriorMutableState, NodeOccupation, NodeState}};



pub async fn register(path: web::Path<String>, data: web::Data<InteriorMutableState>) -> impl Responder {
    let mut counter = data.counter.write().await;
    *counter += 1;
    drop(counter);
    //check if node is already registered
    let node_name = path.into_inner();
    let mut nodes_map = data.known_nodes.write().await;
    if nodes_map.contains_key(&node_name) {
        return HttpResponse::Conflict().body(format!("Node {} already registered", node_name));
    }
    //insert node into known_nodes
    let cloned = node_name.clone();
    let arc_node = Arc::new(node_name);
    nodes_map.insert(arc_node.clone(), NodeState {
        name: cloned.clone(),
        state: NodeOccupation::UNINITIALIZED,
    });
    drop(nodes_map);
    //check if there are values to distribute
    if data.to_distribute.read().await.is_empty() {

        //if no values to distribute, add node to waiting_nodes for later distribution or replication/backup
        let mut waiting_nodes = data.waiting_nodes.write().await;
        waiting_nodes.push(Arc::downgrade(&arc_node));
        drop(waiting_nodes);
        let mut nodes_map = data.known_nodes.write().await;
        let node_state = nodes_map.get_mut(&cloned).unwrap();
        *node_state = NodeState {
            name: cloned.clone(),
            state: NodeOccupation::WAITING,
        };
        drop(nodes_map);
        return HttpResponse::Ok().json(web::Json(NodeRegisterResponse::WAIT));
    }

    //if there are values to distribute, distribute them
    let mut vec_positions = Vec::new();
    let number_to_distribute = *data.expected_values_per_node.read().await;
    let mut to_distribute = data.to_distribute.write().await;
    let to_drain = u32::min(number_to_distribute,to_distribute.len() as u32);

    let mut iter =  to_distribute.drain(0..to_drain as usize);
    while let Some(node_val_to_distribute) = iter.next() {
        vec_positions.push(node_val_to_distribute.clone());
    }
    drop(iter);
    drop(to_distribute);
    let mut nodes_map = data.known_nodes.write().await;
    let node_state = nodes_map.get_mut(&cloned).unwrap();
    *node_state = NodeState {
        name: cloned.clone(),
        state: NodeOccupation::WORKING,
    };
    let string_ref = nodes_map.keys().find(|key| key.as_ref().eq(&cloned));
    let str_ref : Weak<String>;
    if let Some(string_r) = string_ref {
        println!("Node {} is working", &string_r);
        str_ref = Arc::downgrade(string_r);
    }else{
        return HttpResponse::InternalServerError().body("Node not found in known_nodes!");
    }
    drop(nodes_map);
    let mut map = data.map_data.write().await;
    //find key and add reference to that key
  
    for pos in vec_positions.iter(){
        map.insert((pos.x, pos.y), str_ref.clone());
    }
    drop(map);
    let response = NodeRegisterResponse::HANDLE{positions: vec_positions};

    HttpResponse::Ok().json(web::Json(response))

}

pub async fn set_values_per_node(data: web::Data<InteriorMutableState>, value: web::Path<u32>) -> impl Responder {
    let mut expected_values_per_node = data.expected_values_per_node.write().await;
    *expected_values_per_node = value.into_inner();
    HttpResponse::Ok().body(format!("Set expected values per node to: {}", expected_values_per_node))
}



pub async fn initialize(data: web::Data<InteriorMutableState>) -> impl Responder {
    let state = data.into_inner();

    let result = !state.dummy_initialize().await;
    if result {
        return HttpResponse::Conflict().body("Already initialized");
    }
    HttpResponse::Ok().body("Initialized")
}