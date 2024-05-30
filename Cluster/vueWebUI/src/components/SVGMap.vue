<template>
  <div>

    <div
      class="container"
      :style="{ top: position.y + 'px', left: position.x + 'px', transform: 'scale(' + scale + ')' }"
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
      </svg>
    </div>
    <div class="buttons">
      <button id="button1" class="button-27" role="button" @click="extendMap">Extend Map</button>
      <button id="button2" class="button-27" role="button" @click="fetchInformation">Fetch Information</button>
    </div>
  </div>
</template>

<script>



export default {
  name: 'SquareWithRoundedCorners',
  data() {
    return {
      isDragging: false,
      position: { x: 0, y: 0 },
      dragStart: { x: 0, y: 0 },
      testThingie : "test",
      scale: 1,
      corners: new Map(),
      edges: new Map(),
      svgWidth: 200,
      svgHeight: 200,
      informationUrl : "http://localhost:5000",
      lowestX: 0,
      highestX: 1,
      highestY: 1,
      lowestY: 0,
      points: [
      { x: 50, y: 50 },
      { x: 150, y: 150 },
      { x: 250, y: 100 },
      { x: 350, y: 200 },
      { x: 450, y: 50 }
    ],
    };
  },
  mounted() {

    console.log("Mounting!");
    this.position.x = window.innerWidth / 2 - this.svgWidth / 2;
    this.position.y = window.innerHeight / 2 - this.svgHeight / 2;
    this.calculateCornersAndEdges();
  },
  unmounted() {
  },
  methods: {
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
        console.log("X: "+this.position.x+" Y: "+this.position.y);
      }
    },
    stopDrag() {
      this.isDragging = false;
      document.removeEventListener('mousemove', this.onDrag);
      document.removeEventListener('mouseup', this.stopDrag);
    },
    onWheel(event) {
      const zoomFactor = 0.1;
      if (event.deltaY < 0) {
        this.scale += zoomFactor;
      } else {
        this.scale -= zoomFactor;
        if (this.scale < 0.1) this.scale = 0.1;
      }
    },
    onMouseOver() {
      if (!this.isDragging) {
        document.body.style.cursor = 'pointer';
      }
    },
    calculateCornersAndEdges() {
      // Calculate corners based on position and offset
      const offset = 50; // Offset from each edge
      this.corners.set('0/1',{ x: 0 + offset, y: 0 + offset , repr_x: 0 , repr_y: 1});
      this.corners.set('1/1',{ x: 0 + 200 - offset, y: 0 + offset, repr_x: 1 , repr_y: 1});
      this.corners.set('0/0',{ x: 0+ offset, y: 0 + 200 - offset, repr_x: 0 , repr_y: 0});
      this.corners.set('1/0',{ x: 0 + 200 - offset, y: 0 + 200 - offset, repr_x: 1 , repr_y: 0});


      // Calculate edges based on corners
      this.edges.set('0/1-1/1',{ x1: this.corners.get('0/1').x, y1: this.corners.get('0/1').y, x2: this.corners.get('1/1').x, y2: this.corners.get('1/1').y });
      this.edges.set('1/0-1/1',{ x1: this.corners.get('1/0').x, y1: this.corners.get('1/0').y, x2: this.corners.get('1/1').x, y2: this.corners.get('1/1').y });
      this.edges.set('0/0-1/0',{ x1: this.corners.get('0/0').x, y1: this.corners.get('0/0').y, x2: this.corners.get('1/0').x, y2: this.corners.get('1/0').y });
      this.edges.set('0/0-0/1',{ x1: this.corners.get('0/0').x, y1: this.corners.get('0/0').y, x2: this.corners.get('0/1').x, y2: this.corners.get('0/1').y });

    },
    extendMap(){
      
        //random x 
      let x = 0;
      let y = 0;

      while (this.corners.has(x+'/'+y)){
        y++;
      }

      
      const dist_between_corners = 100; // Offset from each edge
      this.increaseSizeIfNecessary(dist_between_corners, x , y);
      let to_add = this.addCorner(x, y);
      if(to_add){
        this.addEdges(x, y);
      }
    },
    addCorner(x, y){
      const offset = 100;
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
        console.log("No neighboring corner found!");
        return false;
      }
      this.corners.set(x+'/'+y ,{ x: x_val  , y: y_val , repr_x: x , repr_y: y});
      return true;
    },
    increaseSizeIfNecessary(offset = 100, x, y){
      if(x<this.lowestX){
        this.lowestX = x;
        this.increaseShiftRight(offset);
      }else if (x > this.highestX){
        this.highestX = x;
        this.svgWidth += offset;
      }
      if(y>this.highestY){
        this.highestY = y;
        this.increaseShiftDown(offset);
      }else if (y < this.lowestY){
        this.lowestY = y;
        this.svgHeight += offset;
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
      this.position.x -= offset;
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
      this.position.y -= offset;
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
    async fetchInformation(){
      const response = await fetch(this.informationUrl + "/getValue/2.3_2.5");
      const data = await response.json();
      console.log(data);
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
  transition: transform 0.1s;
  display: flex;
  justify-content: center;
  align-items: center;

}

  .buttons{
    position: absolute;
    top: 0;
    left: 0;
    background-color: #1A1A1A;
    display: flex;

    justify-content: center;}
  /* CSS */
  .button-27 {
    z-index: 1;
    width: 10px;
    padding: auto;
    appearance: none;
    background-color: #000000;
    border: 2px solid #1A1A1A;
    border-radius: 15px;
    box-sizing: border-box;
    color: #FFFFFF;
    cursor: pointer;
    display: inline-block;
    font-family: Roobert,-apple-system,BlinkMacSystemFont,"Segoe UI",Helvetica,Arial,sans-serif,"Apple Color Emoji","Segoe UI Emoji","Segoe UI Symbol";
    font-size: 16px;
    font-weight: 600;
    line-height: normal;
    margin: 0;
    min-height: 60px;
    min-width: 0;
    outline: none;
    padding: 16px 24px;
    text-align: center;
    text-decoration: none;
    transition: all 300ms cubic-bezier(.23, 1, 0.32, 1);
    user-select: none;
    -webkit-user-select: none;
    touch-action: manipulation;
    width: 100%;
    will-change: transform;
  }
  
  .button-27:disabled {
    pointer-events: none;
  }
  
  .button-27:hover {
    box-shadow: rgba(0, 0, 0, 0.25) 0 8px 15px;
    transform: translateY(-2px);
  }
  
  .button-27:active {
    box-shadow: none;
    transform: translateY(0);
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