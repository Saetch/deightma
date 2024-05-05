use actix_web::{get, post, web, App, HttpResponse, HttpServer, Responder};
use serde::de::value;
use serde_derive::Deserialize;
pub mod calculations;
pub mod debug;

#[get("/")]
async fn hello() -> impl Responder {
    HttpResponse::Ok().body("Hello world!")
}

#[derive(Deserialize)]
struct BicInInput {
    x: f64,
    y: f64,
    arr: String
}

#[get("/calculate")]
async fn calculate(coord: web::Query<BicInInput>) -> impl Responder {
    
    let actual_arr_result = calculations::parse_input_string_to_array(&coord.arr);
    let actual_arr;
    if let Ok(value) = actual_arr_result {
        actual_arr = value;
    } else {
        return HttpResponse::BadRequest().body(actual_arr_result.err().unwrap());
    }
    let result = calculations::bicubic_interpolation(&actual_arr, coord.x, coord.y);
    if let Ok(value) = result {
        return HttpResponse::Ok().body(value.to_string());
    } else {
        return HttpResponse::BadRequest().body(result.err().unwrap());
    }
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {

    debug::test_calculation();
    HttpServer::new(|| {
        App::new()
            .service(hello)
            .service(calculate)
    })
    .bind(("0.0.0.0", 8080))?
    .run()
    .await
}