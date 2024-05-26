use std::sync::Arc;

use rand::Rng;
use serde::Serialize;
use tokio::sync::RwLock;

use crate::communication::Position;

pub type NodeName = String;
pub type DistributeableValue = Position;
pub type HashValue = u16;

#[derive(Debug)]
pub struct InteriorMutableState {
    init: RwLock<bool>,
    pub counter: RwLock<u32>,
    pub known_nodes: RwLock<Vec<Arc<NodeState>>>,
    pub waiting_nodes: RwLock<Vec<Arc<NodeState>>>,  // ->
    pub perlin_noise_seed: RwLock<Option<u32>>,
    pub to_distribute: RwLock<Vec<DistributeableValue>>,
    pub to_replicate: RwLock<Vec<NodeName>>,
    pub expected_values_per_node: RwLock<u32>,
}

#[derive(Debug, Serialize)]
pub struct ImmutableState {
    init: bool,
    pub counter: u32,
    pub known_nodes: Vec<NodeState>,
    pub waiting_nodes: Vec<NodeName>,
    pub perlin_noise_seed: Option<u32>,
    pub to_distribute: Vec<DistributeableValue>,
    pub to_replicate: Vec<NodeName>,
    pub expected_values_per_node: u32,
}

#[derive(Serialize)]
pub struct NodeResponse {
    pub name: NodeName,
    pub hash: HashValue,
}



#[derive(Debug, Clone, Serialize)]
pub struct NodeState{
    pub hash_value: HashValue,
    pub name: NodeName,
    pub state: NodeOccupation,
    pub hash_conflict: bool,

}


impl NodeState {
    pub fn copy(&self ) -> Self {
        Self {
            hash_value: self.hash_value,
            name: self.name.clone(),
            state: self.state.clone(),
            hash_conflict: self.hash_conflict,
        }
    }
}
#[derive(Debug, Clone, Serialize)]
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

impl NodeResponse {
    pub fn from_node_state(node_state: &NodeState) -> Self {
        Self {
            name: node_state.name.clone(),
            hash: node_state.hash_value,
        }
    }
}

impl InteriorMutableState{
    pub fn new() -> Self {
        Self {
            init: RwLock::new(false),
            counter: RwLock::new(0),
            known_nodes: RwLock::new(Vec::new()),
            perlin_noise_seed: RwLock::new(None),
            to_distribute: RwLock::new(Vec::new()),
            expected_values_per_node: RwLock::new(4),
            to_replicate: RwLock::new(Vec::new()),
            waiting_nodes: RwLock::new(Vec::new()),
        }
    }


    //TODO! Add actual logic to determine wether or not new nodes should be added to waiting_nodes or not
    pub fn is_fitted(&self) -> bool {
        false
    }

    pub async fn dummy_initialize(&self) -> bool {
        let mut init = self.init.write().await;
        if *init {
            return false;
        }
        *init = true;
        drop(init); 
        let mut to_distribute = self.to_distribute.write().await;
        assert!(to_distribute.is_empty());
        let mut rand = rand::thread_rng();
        const HEIGHT: usize = 10;
        const WIDTH: usize = 10;

        for x in 0..WIDTH {
            for y in 0..HEIGHT {
                to_distribute.push(Position{x: x as i32, y: y as i32, value: rand.gen_range(0.0..(y +1 )as f64)});
            }
        }
        true
    }


}



impl ImmutableState{
    pub async fn from_interior(state: & InteriorMutableState) -> Self {
        Self {
            init: *state.init.read().await,
            counter: *state.counter.read().await,
            known_nodes: state.known_nodes.read().await.clone().into_iter().map(|x| x.copy()).collect(),
            perlin_noise_seed: *state.perlin_noise_seed.read().await,
            to_distribute: state.to_distribute.read().await.clone(),
            expected_values_per_node: *state.expected_values_per_node.read().await,
            to_replicate: state.to_replicate.read().await.clone(),
            waiting_nodes: state.waiting_nodes.read().await.clone().into_iter().map(|weak| weak.name.to_string()).collect(),
        }
    }
}