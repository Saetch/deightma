use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Clone)]
#[allow(unused)]
pub enum NodeRegisterResponse{
    WAIT,
    HANDLE{
        hash_value: u16,
        positions: Vec<Position>,
    },
    DONE,
}

pub enum NodeCommand{
    HOLDVALUES{
        positions: Vec<Position>,
    }
}
#[derive(Debug, Serialize, Clone, Copy, Deserialize)]
pub struct HashedPosition{
    pub x: i32,
    pub y: i32,
    #[serde(alias = "hashValue")]
    pub hash: u16,
}

#[derive(Debug, Serialize, Clone, Copy, Deserialize)]
pub struct Position{
    pub x: i32,
    pub y: i32,
    pub value: f64,
}


impl From<(i32, i32, f64)> for Position{
    fn from((x, y, value): (i32, i32, f64)) -> Self{
        Self{
            x,
            y,
            value,
        }
    }
    
}