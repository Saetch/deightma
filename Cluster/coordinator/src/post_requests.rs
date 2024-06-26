
use core::hash;
use std::sync::Arc;

use actix_web::{web::{self, Json}, HttpResponse, Responder};
use awc::Client;
use futures::join;
use rand::Rng;

use crate::{communication::{HashedPosition, NodeRegisterResponse, Position}, deal_with_nodes::{self, distribute_value}, state::{InteriorMutableState, NodeOccupation, NodeState}};

pub const HASHER_SERVICE_URL : &str = "http://hasher-service:8080/hash/";

pub async fn register(path: web::Path<String>, data: web::Data<InteriorMutableState>) -> impl Responder {
    let client = Client::default();
    let mut counter = data.counter.write().await;
    *counter += 1;
    drop(counter);
    //check if node is already registered
    let node_name = path.into_inner();
    let fut_map = data.known_nodes.write();
    let url = HASHER_SERVICE_URL.to_string() + &node_name;
    let fut_hash = async {
        let hash_request_response = client.get(url).send().await.unwrap().body().await.unwrap();
        String::from_utf8(hash_request_response.to_vec()).unwrap()
    };
    let (text, mut nodes_map) = join!(fut_hash, fut_map); //use Rust's async feature in order to wait for both futures to complete
    println!("Node hash generated: {:?}", text);
    let hash_value = text.parse::<u16>().unwrap();
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
        println!("Node will be set to WAITING state!");
    }
    let arc_node = Arc::new(node_entry);
    let index = nodes_map.binary_search_by(|x| x.hash_value.cmp(&hash_value)).unwrap_or_else(|x| x);
    nodes_map.insert(index, arc_node.clone());
    drop(nodes_map);
    //check if there are values to distribute
    if data.is_fitted() || is_conflicted {
        println!("Node {} is fitted or has a hash conflict", &node_name);
        //TODO! Test this if it actually works!
        let fut1 = async {
            let mut known_nodes = data.known_nodes.write().await;
            let index = known_nodes.iter().position(|x| x.name == node_name).unwrap();
            known_nodes.remove(index);
            println!("Known nodes is now: {:?}", known_nodes.iter().map(|x| x.name.clone()).collect::<Vec<String>>());
        };
        let fut2 = async {
            let mut waiting_nodes = data.waiting_nodes.write().await;
            waiting_nodes.push(arc_node.clone());
        };
        join!(fut1, fut2);
        return HttpResponse::Ok().json(web::Json(NodeRegisterResponse::WAIT));
    }

    println!("Node {} registered", &node_name);
    

    let mut to_distribute = data.to_distribute.write().await;
    if to_distribute.is_empty() {
        println!("No values to distribute!");
        drop(to_distribute);
        let response = deal_with_nodes::redistribute_values(data.into_inner(), hash_value).await;
        return HttpResponse::Ok().json(web::Json(response));
    }else{
        println!("Values to distribute!");
        let mut known_nodes = data.known_nodes.write().await;
        //binary search for the hash value in the known_nodes
        let index = known_nodes.binary_search_by(|x| x.hash_value.cmp(&hash_value)).unwrap();
        known_nodes[index] = Arc::new(NodeState {
            hash_value,
            name: node_name.clone(),
            state: NodeOccupation::WORKING,
            hash_conflict: is_conflicted,
        });
        return HttpResponse::Ok().json(web::Json(NodeRegisterResponse::HANDLE { hash_value: hash_value, positions: to_distribute.drain(..).collect() }));
    }
    


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

pub async fn upload_value(body: Json<Position>, data: web::Data<InteriorMutableState>) -> impl Responder {
    let client = Client::default();

    let body = body.into_inner();
    let url = HASHER_SERVICE_URL.to_string() + &format!("{}/{}", body.x, body.y);

    let hash_request_response = client.get(url).send().await.unwrap().body().await.unwrap();
    let hash_value = String::from_utf8(hash_request_response.to_vec()).unwrap();
    let lock = data.known_nodes.read().await;
    let index_res = lock.binary_search_by(|x| x.hash_value.cmp(&hash_value.parse::<u16>().unwrap()));
    let index;
    match index_res {
        Ok(i) => index = i,
        Err(i) => index = if i == lock.len() {0} else {i},
    }
    let node = &lock[index];
    let url = format!("http://{}:5552/addValue", node.name);
    let result = client.post(url).send_json(&body).await.unwrap().body().await.unwrap();
    return HttpResponse::Ok().body(String::from_utf8(result.to_vec()).unwrap());

}


pub async fn generate_multiple_values(body: Json<Vec<HashedPosition>>, data: web::Data<InteriorMutableState>) -> impl Responder {
    let mut rng = rand::thread_rng();
    let mut fut_vec = Vec::new();
    for position in body.iter() {
        let fut = distribute_value(position.x, position.y, rng.gen_range(-10.0 .. 10.0), position.hash,  data.clone());
        fut_vec.push(fut);
    }
    let _ = futures::future::join_all(fut_vec).await;
    
    HttpResponse::Ok().body("Values distributed")
}