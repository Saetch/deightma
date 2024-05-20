use std::hash::Hasher;

use actix_web::{web, App, HttpResponse, HttpServer, Responder};
use rustc_hash::FxHasher;


const HASH_RING_SIZE: u16 = 2048;

#[actix_web::main]
async fn main() -> std::io::Result<()>{

    println!("Starting hashing service ...");
    HttpServer::new(move || {
        App::new()
            .route("/", actix_web::web::get().to(pong))
            .route("/ping", actix_web::web::get().to(pong))
            .route("/hash/{x}/{y}", actix_web::web::get().to(hash))
            .route("/hash/{x}", actix_web::web::get().to(hash_single))
            .route("/get_ringhash_size", actix_web::web::get().to(get_hashring_length))
            .route("/get_ringhash_length", actix_web::web::get().to(get_hashring_length))
            .route("/get_hashring_size", actix_web::web::get().to(get_hashring_length))
            .route("/get_hashring_length", actix_web::web::get().to(get_hashring_length))

    })
    .workers(8)
    .keep_alive(None)
    .bind(("0.0.0.0", 8080))?
    .run()
    .await
}

async fn hash(path : web::Path<(i32, i32)>) -> impl Responder {
    let mut hasher = FxHasher::default();
    hasher.write_i32(path.0);
    hasher.write_i32(path.1);
    let val = hasher.finish() as u16 % HASH_RING_SIZE;
    HttpResponse::Ok().body(val.to_string())
}

async fn hash_single(path : web::Path<String>) -> impl Responder {
    let mut hasher = FxHasher::default();
    hasher.write(path.as_bytes());
    let val = hasher.finish() as u16 % HASH_RING_SIZE;
    HttpResponse::Ok().body(val.to_string())
}

async fn get_hashring_length() -> impl Responder {
    HttpResponse::Ok().body(HASH_RING_SIZE.to_string())
}


async fn pong() -> impl Responder {
    HttpResponse::Ok().body("pong")
}

