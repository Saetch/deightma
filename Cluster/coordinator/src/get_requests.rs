use actix_web::{ web, HttpResponse, Responder};

use crate::state::{ImmutableState, InteriorMutableState};



pub async fn get_complete_state(data: web::Data<InteriorMutableState>) -> impl Responder {
    let state = data.into_inner();
    println!("{:?}", state);
    let immutable_state = ImmutableState::from_interior(&state).await;
    HttpResponse::Ok().json(web::Json(immutable_state))
}

pub async fn get_node_for_point(path: web::Path<(i32, i32)>, data: web::Data<InteriorMutableState>) -> impl Responder {
    unimplemented!();
    HttpResponse::Ok().body("Not implemented yet")

}