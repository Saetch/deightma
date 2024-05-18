use std::{rc::Weak, sync::Arc};

use actix_web::{ web, HttpResponse, Responder};

use crate::state::{ImmutableState, InteriorMutableState};



pub async fn get_complete_state(data: web::Data<InteriorMutableState>) -> impl Responder {
    let state = data.into_inner();
    println!("{:?}", state);
    let immutable_state = ImmutableState::from_interior(&state).await;
    HttpResponse::Ok().json(web::Json(immutable_state))
}

pub async fn get_node_for_point(path: web::Path<(i32, i32)>, data: web::Data<InteriorMutableState>) -> impl Responder {

    let state = data.into_inner();
    let (x, y) = path.into_inner();
    let point_map = state.map_data.read().await;
    let node = point_map.get(&(x, y));
    if let Some(node) = node {
        let string = node.upgrade().unwrap().as_ref().clone();
        return HttpResponse::Ok().body(string);
    }
    HttpResponse::Ok().body("Unknown")
}