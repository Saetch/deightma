<template>
  
  <div>

    <div
      class="container"
      :style="{ top: position.y + 'px', left: position.x+ 'px', transform: 'scale(' + scale + ')'}"
      @mousedown="startDrag"
      @wheel="onWheel">
      <svg :width=svgWidth :height=svgHeight
           @mousedown="startDrag"
           @mousemove="onMouseOver"
           @mouseup="endDrag"
           @mouseleave="endDrag">
        <!-- Draw corners -->
        <circle v-for="corner in corners" :key="corner[0]" :cx="corner[1].x" :cy="corner[1].y" r="7" fill="white" />
        <!-- Draw edges -->
        <line v-for="edge in edges" :key="edge[0]" :x1="edge[1].x1" :y1="edge[1].y1" :x2="edge[1].x2" :y2="edge[1].y2" stroke="white" />
        <circle key="currentRequest" :cx="currentRequestPositionInView.x" :cy="currentRequestPositionInView.y" :r=radius fill="green"/>
      </svg>
    </div>
    <div class="sidebar-left">
      <ResultDisplaySidebar :x="currentRequestPosition.x" :y="currentRequestPosition.y" :z="zValue" @update-stepSize="handleUpdateStepSize"/>
    </div>
  </div>
</template>

<script>
import ResultDisplaySidebar from './ResultDisplaySidebar.vue';

export default {
  name: 'SquareWithRoundedCorners',
  components: {
    ResultDisplaySidebar
  },
  data() {
    return {
      currentRequestPosition: {x: 4.5, y: 4.5},
      zValue: 0.0,
      isDragging: false,
      position: { x: 0, y: 0 },
      dragStart: { x: 0, y: 0 },
      scale: 1,
      stepSize: 0.005,
      corners: new Map(),
      edges: new Map(),
      svgWidth: 200,
      svgHeight: 200,
      informationUrl : "http://localhost:5000",
      currentRequestPositionInView: {x: 100, y: 100},
      currentPosCeil: {x: 1, y: 1},
      mapBorders: {x1: 0, x2: 1, y1: 0, y2: 1},
      dist_between_corners: 100,
      translateX: 0,
      translateY: 0,
      directions: {up: false, down: false, left: false, right: false},
      radius: 6,
      initialized: false
    };
  },
  async mounted() {
    window.addEventListener('keypress', (event) => {
      this.handleKeyDown(event);
    });
    window.addEventListener('keyup', (event) => {
      this.handleKeyUp(event);
    });
    console.log("Mounting!");


    await this.calculateCornersAndEdges();
    this.currentRequestPositionInView.x = this.corners.get("0/0").x + this.dist_between_corners * this.currentRequestPosition.x;
    this.currentRequestPositionInView.y = this.corners.get("0/0").y - this.dist_between_corners * this.currentRequestPosition.y;
    this.position.x = window.innerWidth / 2 - this.svgWidth / 2;
    this.position.y = window.innerHeight / 2 - this.svgHeight / 2;
    this.requestNewData();
    this.initialized = true;
    this.UpdateRequestview();
    console.log("Running webUi with an expected kubernetes connection!");
    setInterval(this.MoveRequest, 1000/10);
  },
  unmounted() {
  },
  methods: {
    handleUpdateStepSize(newStepSize) {
      this.stepSize = newStepSize;
    },
    handleKeyDown(event) {
      switch (event.key) {
        case 'w':
        case 'W':
          this.directions.up = true;
          break;
        case 'a':
        case 'A':
          this.directions.left = true;
          break;
        case 's':
        case 'S':
          this.directions.down = true;
          break;
        case 'd':
        case 'D':
          this.directions.right += true;
          break;
        default:
          break;
      }
    },
    handleKeyUp(event) {
      switch (event.key) {
        case 'w':
        case 'W':
          this.directions.up = false;
          break;
        case 'a':
        case 'A':
          this.directions.left = false;
          break;
        case 's':
        case 'S':
          this.directions.down = false;
          break;
        case 'd':
        case 'D':
          this.directions.right = false;
          break;
        default:
          break;
      }
    },
    async MoveRequest(){
      let xChange = 0;
      let yChange = 0;
      if(this.directions.up){
        yChange += this.stepSize;
      }
      if(this.directions.down){
        yChange -= this.stepSize;
      }
      if(this.directions.left){
        xChange -= this.stepSize;
      }
      if(this.directions.right){
        xChange += this.stepSize;
      }
      this.currentRequestPosition.x += xChange;
      this.currentRequestPosition.y += yChange;
      if ( xChange != 0 || yChange != 0){
        await this.UpdateRequestview();
      }
    },
    async UpdateRequestview(newData = true){
      const xCeil = Math.ceil(this.currentRequestPosition.x);
      const yCeil = Math.ceil(this.currentRequestPosition.y);
      if (xCeil != this.currentPosCeil.x || yCeil != this.currentPosCeil.y) {
        this.currentPosCeil.x = xCeil;
        this.currentPosCeil.y = yCeil;
        if(newData ){
          if(!this.hasNeededCorners(xCeil, yCeil)){
            this.requestNewCeilData();
          }else {
            this.requestNewData();
          }
        }
      } else {
        if(newData){
          this.requestNewData();
        }
      }
      if(this.initialized){
        this.currentRequestPositionInView.x = this.corners.get("0/0").x + this.dist_between_corners * this.currentRequestPosition.x;
      this.currentRequestPositionInView.y = this.corners.get("0/0").y - this.dist_between_corners * this.currentRequestPosition.y;
      }

    },
    hasNeededCorners(x, y){
      for( let i = x - 2; i <= x + 2; i++){
        for( let j = y - 2; j <= y + 2; j++){
          if(!this.corners.has(i+'/'+j)){
            return false;
          }
        }
      }
      return true;
    },
    async requestNewData(){
      const response = await fetch(this.informationUrl + "/getValue/" + this.currentRequestPosition.x + "/" + this.currentRequestPosition.y);
      const data = await response.json();
      this.zValue = data.value;  
    },
    async requestNewCeilData(){
      const response = await fetch(this.informationUrl + "/getValueAutoInc/" + this.currentRequestPosition.x + "/" + this.currentRequestPosition.y+"/3");
      const data = await response.json();
      this.zValue = data.value.value;
      data.addedCorners.forEach(corner => {
        this.addCorner(corner.x, corner.y);
        this.addEdges(corner.x, corner.y);
      });
    },
    startDrag(event) {
      this.isDragging = true;
      this.dragStart.x = event.clientX - this.position.x;
      this.dragStart.y = event.clientY - this.position.y;
      document.addEventListener('mousemove', this.onDrag);
      document.addEventListener('mouseup', this.stopDrag);
    },
    onDrag(event) {
      if (this.isDragging) {
        this.position.x = event.clientX - this.dragStart.x;
        this.position.y = event.clientY - this.dragStart.y;
      }
    },
    stopDrag() {
      this.isDragging = false;
      document.removeEventListener('mousemove', this.onDrag);
      document.removeEventListener('mouseup', this.stopDrag);
    },
    onWheel(event) {
    const zoomFactor = 0.1;
    const minScale = 0.1;
    
    let newScale = this.scale;
    if (event.deltaY < 0) {
      newScale += zoomFactor;
    } else {
      newScale -= zoomFactor;
      if (newScale < minScale) newScale = minScale;
    }


    let xDistFromCenter = (event.clientX - (this.position.x) - this.svgWidth / 2.0);
    let yDistFromCenter = (event.clientY - (this.position.y) - this.svgHeight / 2.0);
    let newXDistFromCenter = xDistFromCenter * (newScale / this.scale);
    let newYDistFromCenter = yDistFromCenter * (newScale / this.scale);


    this.scale = newScale;
    this.position.x -= newXDistFromCenter - xDistFromCenter;
    this.position.y -= newYDistFromCenter - yDistFromCenter;
    this.radius = 6 / newScale;
  },
    onMouseOver() {
      if (!this.isDragging) {
        document.body.style.cursor = 'pointer';
      }
    },
     async calculateCornersAndEdges() {
      const response = await fetch(this.informationUrl+"/getAllSavedValues");

      // Check if the response is OK (status code 200-299)
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

        // Parse the JSON response
      const data = await response.json();
      data.forEach(element => {
        this.addCorner(element.x, element.y);
        this.addEdges(element.x, element.y);
        
        
      });
    },
    addCorner(x, y){
      const offset = 100;

      this.increaseSizeIfNecessary(x, y, 100);
      //find correct value for x and y
      let x_val = 0;
      let y_val = 0;
      if(this.corners.has(x+'/'+y)){
        console.log("Corner already exists!");
        return;
      }
      let neighboring_corner = false;
      
      if(this.corners.has((x-1)+'/'+y)){
        let neighbor_obj = this.corners.get((x-1)+'/'+y);
        x_val = neighbor_obj.x + offset;
        y_val = neighbor_obj.y;
        neighboring_corner = true;
      }
      if(this.corners.has(x+'/'+(y+1))){
        let neighbor_obj = this.corners.get(x+'/'+(y+1));
        y_val = neighbor_obj.y + offset;
        x_val = neighbor_obj.x;
        neighboring_corner = true;
      }
      if (this.corners.has((x+1)+'/'+y)){
        x_val = this.corners.get((x+1)+'/'+y).x - offset;
        y_val = this.corners.get((x+1)+'/'+y).y;
        neighboring_corner = true;
      }
      if(this.corners.has(x+'/'+(y-1))){
        y_val = this.corners.get(x+'/'+(y-1)).y - offset;
        x_val = this.corners.get(x+'/'+(y-1)).x;
        neighboring_corner = true;
      }
      if(!neighboring_corner){
        this.addDanglingCorner(x, y);
      }else{
        this.corners.set(x+'/'+y ,{ x: x_val  , y: y_val , repr_x: x , repr_y: y});
      }
      return true;
    },
    increaseSizeIfNecessary( x, y, offset = 100){
      if(x<this.mapBorders.x1){
        for ( let i = 0; i < this.mapBorders.x1 - x; i++){
          this.increaseShiftRight(offset);
        }
        this.mapBorders.x1 = x;
      }else if (x > this.mapBorders.x2){
        this.svgWidth += offset * (x - this.mapBorders.x2);
        this.position.x += offset * (this.scale /2 - 0.5) * (x - this.mapBorders.x2);
        this.mapBorders.x2 = x;
      }
      if(y>this.mapBorders.y2){
        for ( let i = 0; i < y - this.mapBorders.y2; i++){
          this.increaseShiftDown(offset);
        }
        this.mapBorders.y2 = y;
      }else if (y < this.mapBorders.y1){
        this.svgHeight += offset * (this.mapBorders.y1 - y);

        this.position.y += offset * (this.scale /2 - 0.5) * (this.mapBorders.y1 - y);
        this.mapBorders.y1 = y;
      }

    },
    increaseShiftRight(offset = 100){
      this.corners.forEach(corner => {
        corner.x += offset;
      });
      this.edges.forEach(edge => {
        edge.x1 += offset;
        edge.x2 += offset;
      });
      this.position.x -= offset * (this.scale /2  + 0.5);
      this.UpdateRequestview(false);
      this.svgWidth += offset;
    },
    increaseShiftDown(offset = 100){
      this.corners.forEach(corner => {
        corner.y += offset;
      });
      this.edges.forEach(edge => {
        edge.y1 += offset;
        edge.y2 += offset;
      });
      this.position.y -= offset * (this.scale /2 + 0.5);
      this.UpdateRequestview(false);
      this.svgHeight += offset;
    },
    addEdges(x, y){
      let current_corner = this.corners.get(x+'/'+y);
      if (this.corners.has((x-1)+'/'+y)){
        let neighbor_corner = this.corners.get((x-1)+'/'+y);
        this.edges.set(x+'/'+y+'-'+(x-1)+'/'+y,{ x1: current_corner.x, y1: current_corner.y, x2: neighbor_corner.x, y2: neighbor_corner.y });
      }
      if (this.corners.has(x+'/'+(y+1))){
        let neighbor_corner = this.corners.get(x+'/'+(y+1));
        this.edges.set(x+'/'+y+'-'+x+'/'+(y+1),{ x1: current_corner.x, y1: current_corner.y, x2: neighbor_corner.x, y2: neighbor_corner.y });
      }
      if (this.corners.has((x+1)+'/'+y)){
        let neighbor_corner = this.corners.get((x+1)+'/'+y);
        this.edges.set(x+'/'+y+'-'+(x+1)+'/'+y,{ x1: current_corner.x, y1: current_corner.y, x2: neighbor_corner.x, y2: neighbor_corner.y });
      }
      if (this.corners.has(x+'/'+(y-1))){
        let neighbor_corner = this.corners.get(x+'/'+(y-1));
        this.edges.set(x+'/'+y+'-'+x+'/'+(y-1),{ x1: current_corner.x, y1: current_corner.y, x2: neighbor_corner.x, y2: neighbor_corner.y });
      }
    },
    addDanglingCorner(x,y){
      const startingOffset = 50;
      const offsetBetweenCorners = 100;
      const firstX = this.mapBorders.x1;
      const firstY = this.mapBorders.y2;
      const indexX = x - firstX;
      const indexY = firstY - y;
      this.corners.set(x+'/'+y ,{ x: startingOffset + offsetBetweenCorners * indexX  , y: startingOffset + offsetBetweenCorners * indexY , repr_x: x , repr_y: y});
    }

  },
};
</script>

<style scoped>
/* center the svg */
.container {
  z-index: 0;
  position: absolute;
  cursor: grab;
  transition: scale 0.1s;
  display: flex;
  justify-content: center;
  align-items: center;

}

  

.container:active {
  cursor: grabbing;
}

.point {
  fill: red;
  stroke: black;
  stroke-width: 1px;
}
.path {
  fill: none;
  stroke: blue;
  stroke-width: 2px;
}
</style>