use std::{ sync::Arc};

use actix_web::web;

use crate::{communication::{NodeRegisterResponse, Position}, state::{InteriorMutableState, NodeState}};


#[inline(always)]
pub async fn redistribute_values(data: Arc<InteriorMutableState>, hash_value: u16) -> NodeRegisterResponse {
    println!("Redistributing values for node with hash value: {}", hash_value);
    let positions_vec;
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
        let to_distribute_from = if index != all_known_nodes.len()-1 {index + 1} else {0} ;  //the values are distributed to the next hash value, so getting the next one after the  current one yields the one that might have a share of values to redistribute
        let node_to_distribute_from = &all_known_nodes[to_distribute_from];
        let mut worked_communication = false; 
        let mut response = Err(awc::error::SendRequestError::Timeout);
        for _ in 0..=6{
            let url = format!("http://{}:5552/deleteSavedValuesBelow/{}", node_to_distribute_from.name,  hash_value.to_string());
            println!("URL: {}", url);
            //If trying to deal with nonexistent or non-responding node, edit here!
            response = awc::Client::default().post(url).send().await;
            if response.is_ok(){
                break;
            }else{
                println!("Failed to communicate with node: {} ... retrying ...", node_to_distribute_from.name);
                std::thread::sleep(std::time::Duration::from_secs(5));
            }


        }
        let mut resp: bytes::Bytes;
        if let Ok(mut response) = response{
            resp = response.body().await.unwrap();
            println!("Response: {:?}", response);
        }else{
            println!("Expected aborting ... ");
            panic!("Node with hash value {} not found in known_nodes with data: {:?}", hash_value, data);
        }
        let values_to_distribute : Vec<Position> = serde_json::from_slice(&resp).unwrap();
        println!("Response: {:?}", resp);
        println!("Values to distribute: {:?}", values_to_distribute);
        //If trying to deal with nonexistent or non-responding node, edit here!
        let values_to_distribute : Vec<Position> = serde_json::from_slice(&resp).unwrap();
        println!("Values to distribute: {:?}", values_to_distribute);
        positions_vec = values_to_distribute;
    }else{
        panic!("Node with hash value {} not found in known_nodes with data: {:?}", hash_value, data);
    }
    

    NodeRegisterResponse::HANDLE{
        hash_value,
        positions: positions_vec,
    }
}


pub async fn distribute_value(x: i32, y: i32, value: f64, hash: u16, data: web::Data<InteriorMutableState>){
    let known_nodes = data.known_nodes.read().await;
    let index = known_nodes.binary_search_by(|x| x.hash_value.cmp(&hash));
    let pos_body = Position{x, y, value};
    let Ok(i) = index else{
        let index_in_known_nodes = if index.err().unwrap() < known_nodes.len() {index.err().unwrap()} else {0};
        let url = format!("http://{}:5552/addValue", known_nodes[index_in_known_nodes].name);
        let client = awc::Client::default();
        let response = client.post(url).send_json(&pos_body).await.unwrap().body().await.unwrap();
        println!("Response: {:?}", response);
        return;
    };
    let index_in_known_nodes = if i < known_nodes.len() {i} else {0};
    let url = format!("http://{}:5552/addValue", known_nodes[index_in_known_nodes].name);
    let client = awc::Client::default();
    let response = client.post(url).send_json(&pos_body).await.unwrap().body().await.unwrap();
    println!("Response: {:?}", response);
    return;
    
}