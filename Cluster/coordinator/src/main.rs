use actix_web::{get, post, web, App, HttpResponse, HttpServer, Responder};



async fn manual_hello() -> impl Responder {
    HttpResponse::Ok().body("Hey there!")
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    HttpServer::new(|| {
        App::new()
            .service(web::scope("/organize")
                .route("/create", web::post().to(|| HttpResponse::Ok()))
                .route("/delete", web::delete().to(|| HttpResponse::Ok()))
                .route("/update", web::put().to(|| HttpResponse::Ok()))
                .route("/read", web::get().to(|| HttpResponse::Ok()))
                .route("/hey", web::get().to(manual_hello)),
    )
    })
    .bind(("0.0.0.0", 8080))?
    .run()
    .await
}