use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Clone)]
pub enum NodeRegisterResponse{
    WAIT,
    HANDLE{
        positions: Vec<Position>,
    },
    DONE,
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