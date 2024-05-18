use actix_web::{web, App, HttpResponse, HttpServer, Responder, middleware::Logger};
use env_logger::Env;
use post_requests::{initialize, register, set_values_per_node};
mod state;
mod post_requests;
mod get_requests;
mod communication;
async fn manual_hello() -> impl Responder {
    HttpResponse::Ok().body("Hey there!")
}

async fn ping() -> impl Responder {
    HttpResponse::Ok().body("pong")
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let share_state = state::InteriorMutableState::new();
    let arc_share_state = web::Data::new(share_state);

    env_logger::init_from_env(Env::default().default_filter_or("info"));

    
    HttpServer::new(move || {
        App::new()
            .wrap(Logger::default())
            .wrap(Logger::new("%a %{User-Agent}i"))
            .app_data(arc_share_state.clone())
            .service(web::scope("/organize")  //get requests are for debug purposes, since it is easier to use a browser to check the state without postman or similar
                .route("/register/{node_name}", web::post().to( register ))
                .route("/register_debug/{node_name}", web::get().to(register))
                .route("/set_expected_values_per_node/{values}", web::put().to(set_values_per_node))
                .route("/set_expected_values_per_node/{values}", web::get().to(set_values_per_node))
                .route("/initialize", web::post().to(initialize))
                .route("/initialize", web::get().to(initialize))
                .route("/get_complete_state", web::get().to(get_requests::get_complete_state))
                .route("/get_node/{x}/{y}", web::get().to(get_requests::get_node_for_point))
                .route("/hey", web::get().to(manual_hello)),
    )
            .route("/ping", web::get().to(ping))
    })
    .workers(8)
    .keep_alive(None)
    .bind(("0.0.0.0", 8080))?
    .run()
    .await
}