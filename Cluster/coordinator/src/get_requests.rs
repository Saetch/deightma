use actix_web::{ web, HttpResponse, Responder};

use crate::state::{ImmutableState, InteriorMutableState};



pub async fn get_complete_state(data: web::Data<InteriorMutableState>) -> impl Responder {
    let state = data.into_inner();
    println!("{:?}", state);
    let immutable_state = ImmutableState::from_interior(&state).await;
    HttpResponse::Ok().json(web::Json(immutable_state))
}