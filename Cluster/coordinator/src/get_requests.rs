
use actix_web::{ web, HttpResponse, Responder};
use futures::{future::join_all, join};

use crate::{post_requests::HASHER_SERVICE_URL, state::{ImmutableState, InteriorMutableState, NodeResponse}};



pub async fn get_complete_state(data: web::Data<InteriorMutableState>) -> impl Responder {
    let state = data.into_inner();
    println!("{:?}", state);
    let immutable_state = ImmutableState::from_interior(&state).await;
    HttpResponse::Ok().json(web::Json(immutable_state))
}

pub async fn get_all_nodes(data: web::Data<InteriorMutableState>) -> impl Responder {
    let state = data.into_inner();
    let known_nodes = state.known_nodes.read().await;
    HttpResponse::Ok().json(web::Json(known_nodes.iter().map(|x| NodeResponse::from_node_state(x)).collect::<Vec<NodeResponse>>()))
}

pub async fn debug_distribution(data: web::Data<InteriorMutableState>) -> impl Responder {

    let mut fut_vec = Vec::new();
    for i in 0..4 {
        for j in 0..4 {
            let p = web::Path::from((i, j));
            let fut = get_node_for_point(p, data.clone());
            fut_vec.push(fut);
        }
    }
    let result_vec = join_all(fut_vec).await;
    for result in result_vec {
        println!("Found nodes for all 16 points!");
    }
    HttpResponse::Ok().body("Debugging distribution, see log")
}

pub async fn get_node_for_point(path: web::Path<(i32, i32)>, data: web::Data<InteriorMutableState>) -> impl Responder {
    let (x, y) = path.into_inner();
    let state = data.into_inner();
    let known_nodes_fut = state.known_nodes.read();
    let client = awc::Client::default();
    let url = HASHER_SERVICE_URL.to_string() + &format!("{}/{}", x, y);
    let fut_hash = async {
        let hash_request_response = client.get(url).send().await.unwrap().body().await.unwrap();
        String::from_utf8(hash_request_response.to_vec()).unwrap()
    };
    let (hash_value_str, known_nodes) = join!(fut_hash, known_nodes_fut);
    if known_nodes.is_empty() {
        return HttpResponse::Ok().body("No nodes registered yet");
    }
    let hash_value = hash_value_str.parse::<u16>().unwrap();
    let index = known_nodes.binary_search_by(|x| x.hash_value.cmp(&hash_value));
    if let Ok(ind) = index {
        println!("Node with hash value {} found in known node: {:?}", hash_value, known_nodes[ind].name);
        return HttpResponse::Ok().json(web::Json(known_nodes[ind].name.clone()));
    }else{
        let ind = index.err().unwrap();
        let ind = if ind == known_nodes.len() {0} else {ind};
        println!("Node with hash value {} not found, returning name: {:?}", hash_value, known_nodes[ind].name.clone());

        return HttpResponse::Ok().body(known_nodes[ind].name.clone());
    }

}