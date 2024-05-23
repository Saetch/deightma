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
pub struct Position{
    pub(crate) x: i32,
    pub(crate) y: i32,
    pub(crate) value: f64,
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