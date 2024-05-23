use std::{error::Error, fmt::{self, Display, Formatter}, hash::Hasher, time::SystemTime};

use actix_web::{web::{self, Query}, App, HttpResponse, HttpServer, Responder};
use rustc_hash::FxHasher;
use serde::de::value;
use std::time::Duration;


const HASH_RING_SIZE: u16 = 16384;

#[actix_web::main]
async fn main() -> std::io::Result<()>{

    println!("Starting hashing service ...");
    HttpServer::new(move || {
        App::new()
            .route("/", actix_web::web::get().to(pong))
            .route("/ping", actix_web::web::get().to(pong))
            .route("/hash/{x}/{y}", actix_web::web::get().to(hash))
            .route("/hash/{x}", actix_web::web::get().to(hash_single))
            .route("/hash_multiple", actix_web::web::get().to(hash_multiple))
            .route("/get_ringhash_size", actix_web::web::get().to(get_hashring_length))
            .route("/get_ringhash_length", actix_web::web::get().to(get_hashring_length))
            .route("/get_hashring_size", actix_web::web::get().to(get_hashring_length))
            .route("/get_hashring_length", actix_web::web::get().to(get_hashring_length))
    })
    .workers(8)
    .keep_alive(Duration::from_millis(10))
    .bind(("0.0.0.0", 8080))?
    .run()
    .await
}

async fn hash(path : web::Path<(i32, i32)>) -> impl Responder {
    println!("Called at: {:?}", SystemTime::now().duration_since(SystemTime::UNIX_EPOCH).unwrap().as_millis());
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



async fn hash_multiple(query_info: web::Query<QueryInfo>) -> Result <impl Responder, Box<dyn Error>> {
    let values = vec_from_query_string(&query_info)?;
    let mut ret_vec = Vec::new();
    println!("Received: {:?}", values);
    for pos in values.iter() {
        let mut hasher = FxHasher::default();
        hasher.write_i32(pos.x);
        hasher.write_i32(pos.y);
        let val = hasher.finish() as u16 % HASH_RING_SIZE;
        ret_vec.push(HashedPosition{x: pos.x, y: pos.y, hash: val});
    }
    Ok(HttpResponse::Ok().json(ret_vec))
}


fn vec_from_query_string(query_string: &QueryInfo) -> Result<Vec<Position>, Box<dyn Error>> {
    let mut vec = Vec::new();
    let string_positions = query_string.vec.split(";");
    for string_position in string_positions {
        let mut parse_content = Vec::new();
        let mut content = string_position.split(",");
        while let Some(x) = content.next() {
            let mut chars = x.chars().into_iter();
            let mut current_field = None;
            let mut current_str = "".to_string();

            while let Some(y) = chars.next() {
                if y == ':' {
                    match current_str.as_str(){
                        "x" => {
                            current_field = Some(PositionField::X{value: None});
                        },
                        "y" => {
                            current_field = Some(PositionField::Y{value: None});
                        },
                        _ => {
                            return Err(Box::new(ParseError{message: "Invalid field or seperator trying to read the array".to_string()}));
                        }
                    }
                    current_str.clear();
                    continue;
                }
                current_str.push(y);
            }
            let value : i32;
            if let Some(field) = current_field {
                value = current_str.parse::<i32>()?;
                match field {
                    PositionField::X{value: _} => {
                        parse_content.push(PositionField::X{value: Some(value)});
                    },
                    PositionField::Y{value: _} => {
                        parse_content.push(PositionField::Y{value: Some(value)});
                    },
                }
            }
        }

        let mut x = 0;
        let mut y = 0;
        let mut added_x = false;
        let mut added_y = false;
        for fields in &parse_content {

            match fields {
                PositionField::X{value: Some(val)} => {
                    x = *val;
                    added_x = true;
                },
                PositionField::Y{value: Some(val)} => {
                    y = *val;
                    added_y = true;
                },
                _ => {
                    return Err(Box::new(ParseError{message: "Invalid field or separator when trying to fill Position structs".to_string()}));
                }
            }
        }
        if parse_content.len() != 2 || !added_x || !added_y{
            return Err(Box::new(ParseError{message: "Invalid number of fields in the array".to_string()}));
        }
        vec.push(Position{x: x, y: y});

        parse_content.clear();
    }
    Ok(vec)
}


#[derive(Debug)]
struct ParseError {
    message: String
}

impl Error for ParseError {
    fn description(&self) -> &str {
        &self.message
    }
}

impl Display for ParseError {
    fn fmt(&self, f: &mut Formatter) -> fmt::Result {
        write!(f, "{}", self.message)
    }
}


#[derive(serde_derive::Deserialize,serde_derive::Serialize, Debug)]
struct QueryInfo {
    vec: String
}

#[derive(serde_derive::Deserialize,serde_derive::Serialize, Debug)]
struct Position {
    x: i32,
    y: i32,
}

enum PositionField {
    X{value: Option<i32>},
    Y{value: Option<i32>},
}

#[derive(serde_derive::Serialize, Debug)]
struct HashedPosition{
    x: i32,
    y: i32,
    hash: u16,
}

async fn get_hashring_length() -> impl Responder {
    HttpResponse::Ok().body(HASH_RING_SIZE.to_string())
}


async fn pong() -> impl Responder {
    HttpResponse::Ok().body("pong")
}

