
use std::{borrow::BorrowMut, hash::Hasher, sync::{Arc, Weak}};

use actix_web::{web, HttpResponse, Responder};
use rustc_hash::FxHasher;
use serde::de::value;

use crate::{balancing::distribute_value, communication::NodeRegisterResponse, state::{InteriorMutableState, NodeOccupation, NodeState, HASH_RING_SIZE}};



pub async fn register(path: web::Path<String>, data: web::Data<InteriorMutableState>) -> impl Responder {
    let mut counter = data.counter.write().await;
    *counter += 1;
    drop(counter);
    //check if node is already registered
    let node_name = path.into_inner();
    let mut nodes_map = data.known_nodes.write().await;
    let mut hasher = FxHasher::default();
    hasher.write(node_name.as_bytes());
    let hash_value = hasher.finish() as u16 % HASH_RING_SIZE;
    drop(hasher);
    if nodes_map.iter().any(|x| x.name == node_name){
        return HttpResponse::Conflict().body("Node already registered");
    }
    let is_conflicted;
    if nodes_map.iter().any(|x| x.hash_value == hash_value){
        is_conflicted = true;
    }else{
        is_conflicted = false;
    }
    //insert node into known_nodes
    let node_entry = NodeState {
        hash_value,
        name: node_name.clone(),
        state: NodeOccupation::UNINITIALIZED,
        hash_conflict: is_conflicted,
    };
    if is_conflicted {
        println!("Node {} has a hash conflict", &node_name);
        todo!("Handle hash conflict");
    }
    let arc_node = Arc::new(node_entry);
    nodes_map.push(arc_node.clone());
    drop(nodes_map);
    //check if there are values to distribute
    if data.is_fitted() {
        //TODO! Test this if it actually works!
        let new_node = NodeState {
            hash_value,
            name: node_name.clone(),
            state: NodeOccupation::WAITING,
            hash_conflict: is_conflicted,
        };
        let mut known_nodes = data.known_nodes.write().await;
        let index = known_nodes.iter().position(|x| x.name == node_name).unwrap();
        known_nodes[index] = Arc::new(new_node);
        known_nodes.sort_by(|a, b| a.hash_value.cmp(&b.hash_value));
        println!("Known nodes is now: {:?}", known_nodes.iter().map(|x| x.name.clone()).collect::<Vec<String>>());
        drop(known_nodes);
        //if no values to distribute, add node to waiting_nodes for later distribution or replication/backup
        let mut waiting_nodes = data.waiting_nodes.write().await;
        waiting_nodes.push(Arc::downgrade(&arc_node));
        drop(waiting_nodes);

        return HttpResponse::Ok().json(web::Json(NodeRegisterResponse::WAIT));
    }

    let mut to_distribute = data.to_distribute.write().await;
    return HttpResponse::Ok().json(web::Json(NodeRegisterResponse::HANDLE { positions: to_distribute.drain(..).collect() }));
    


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

pub async fn upload_value(path: web::Path<(i32, i32, f64)>, data: web::Data<InteriorMutableState>) -> impl Responder {
    let (x, y, value) = path.into_inner();
    if data.known_nodes.read().await.is_empty() {
        let mut to_distribute = data.to_distribute.write().await;
        to_distribute.push((x, y, value).into());
        HttpResponse::Ok().body("Value uploaded, waiting for node to be available")
    }else{
        distribute_value(x, y, value, data).await;
        HttpResponse::Ok().body("Value uploaded and distributed")
    }

}