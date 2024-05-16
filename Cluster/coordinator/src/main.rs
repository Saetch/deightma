use actix_web::{web, App, HttpResponse, HttpServer, Responder, middleware::Logger};
use env_logger::Env;
use post_requests::register;
mod state;
mod post_requests;
mod get_requests;

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
            .service(web::scope("/organize")
                .route("/register/{node_name}", web::post().to( register ))
                .route("/register_debug/{node_name}", web::get().to(register))
                .route("/delete", web::delete().to(|| HttpResponse::Ok()))
                .route("/update", web::put().to(|| HttpResponse::Ok()))
                .route("/read", web::get().to(|| HttpResponse::Ok()))
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