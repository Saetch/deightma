use std::collections::HashMap;

use tokio::sync::RwLock;


pub struct InteriorMutableState {
    pub counter: u32,
    pub known_nodes: RwLock<HashMap<String, NodeState>>,
    pub map_data: RwLock<HashMap<(i32, i32), String>>,
    pub perlin_noise_seed: RwLock<Option<u32>>,
    pub to_distribute: RwLock<Vec<DistributeableValue>>,
}

pub struct DistributeableValue {
    x: i32,
    y: i32,
    value: f64,
    replication: u32,
}


pub struct NodeState{
    pub name: String,
    pub state: RwLock<NodeOccupation>,

}

pub enum NodeOccupation {
    UNINITIALIZED,
    WAITING,
    WORKING,
    BACKUP,
    DOWN,
    NORESPONSE{
        since: u32,
    }
}

impl InteriorMutableState{
    pub fn new() -> Self {
        Self {
            counter: 0,
            known_nodes: RwLock::new(HashMap::new()),
            map_data: RwLock::new(HashMap::new()),
            perlin_noise_seed: RwLock::new(None),
            to_distribute: RwLock::new(Vec::new()),

        }
    }
}