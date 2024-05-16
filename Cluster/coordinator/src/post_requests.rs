use actix_web::{web, HttpResponse, Responder};
use tokio::sync::RwLock;

use crate::state::{InteriorMutableState, NodeOccupation, NodeState};



pub async fn register(path: web::Path<String>, data: web::Data<InteriorMutableState>) -> impl Responder {
    
    let node_name = path.into_inner();
    let mut nodes_map = data.known_nodes.write().await;
    if nodes_map.contains_key(&node_name) {
        return HttpResponse::Conflict().body(format!("Node {} already registered", node_name));
    }
    let cloned = node_name.clone();
    nodes_map.insert(node_name, NodeState {
        name: cloned.clone(),
        state: RwLock::new(NodeOccupation::UNINITIALIZED),
    });
    drop(nodes_map);

    

    HttpResponse::Ok().body(format!("Registering node: {}", cloned))

}