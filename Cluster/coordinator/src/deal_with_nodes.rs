use std::sync::Arc;

use actix_web::web;

use crate::{communication::{NodeRegisterResponse, Position}, state::{InteriorMutableState, NodeState}};


#[inline(always)]
pub async fn redistribute_values(data: Arc<InteriorMutableState>, hash_value: u16) -> NodeRegisterResponse {
    println!("Redistributing values for node with hash value: {}", hash_value);
    let mut positions_vec : Vec<Position> = Vec::new();
    let mut all_known_nodes = data.known_nodes.write().await;
    let index = all_known_nodes.binary_search_by(|x| x.hash_value.cmp(&hash_value));
    if let Ok(index) = index {
        let (name, hash_conflict) = {
            let node = &all_known_nodes[index];
            (node.name.clone(), node.hash_conflict)
        };
        all_known_nodes[index] = Arc::new(NodeState{
            hash_value,
            name,
            state: crate::state::NodeOccupation::WORKING,
            hash_conflict,
        });
        let to_distribute_from = if index != 0 {index -1} else {all_known_nodes.len()-1} ;  //the values are distributed to the next hash value, so getting the one before the current one yields the one that might have a share of values to redistribute
        let node_to_distribute_from = &all_known_nodes[to_distribute_from];
        let url = format!("http://{}:5552/getAllSavedValues", node_to_distribute_from.name);
        println!("URL: {}", url);
        let response = awc::Client::default().get(url).send().await.unwrap().body().await.unwrap();
        let values_to_distribute : Vec<Position> = serde_json::from_slice(&response).unwrap();
        println!("Values to distribute: {:?}", values_to_distribute);
    }else{
        panic!("Node with hash value {} not found in known_nodes with data: {:?}", hash_value, data);
    }
    

    NodeRegisterResponse::HANDLE{
        hash_value,
        positions: positions_vec,
    }
}


pub async fn distribute_value(x: i32, y: i32, value: f64, data: web::Data<InteriorMutableState>){


    println!("Distributing value: ({}, {}, {})", x, y, value);
    println!("distribute_value called but not yet implemented!");
    
}