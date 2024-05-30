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
        <circle v-for="corner in corners" :key="corner.id" :cx="corner.x" :cy="corner.y" r="7" fill="white" />
        <!-- Draw edges -->
        <line v-for="edge in edges" :key="edge.id" :x1="edge.x1" :y1="edge.y1" :x2="edge.x2" :y2="edge.y2" stroke="white" />
      </svg>
    </div>
    <div class="buttons">
      <button id="button1" class="button-27" role="button" @click="extendMap">Extend Map</button>
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
      corners: [],
      edges: [],
      svgWidth: 200,
      svgHeight: 200,
    };
  },
  mounted() {
    setInterval(() => {
      this.extendMap();
    }, 1000);
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
      this.corners = [
        { id: 'top-left', x: 0 + offset, y: 0 + offset , repr_x: 0 , repr_y: 1},
        { id: 'top-right', x: 0 + 200 - offset, y: 0 + offset, repr_x: 1 , repr_y: 1},
        { id: 'bottom-left', x: 0+ offset, y: 0 + 200 - offset, repr_x: 0 , repr_y: 0},
        { id: 'bottom-right', x: 0 + 200 - offset, y: 0 + 200 - offset, repr_x: 1 , repr_y: 0},
      ];

      // Calculate edges based on corners
      this.edges = [
        { id: 'top-edge', x1: this.corners[0].x, y1: this.corners[0].y, x2: this.corners[1].x, y2: this.corners[1].y },
        { id: 'right-edge', x1: this.corners[1].x, y1: this.corners[1].y, x2: this.corners[3].x, y2: this.corners[3].y },
        { id: 'bottom-edge', x1: this.corners[3].x, y1: this.corners[3].y, x2: this.corners[2].x, y2: this.corners[2].y },
        { id: 'left-edge', x1: this.corners[2].x, y1: this.corners[2].y, x2: this.corners[0].x, y2: this.corners[0].y },
      ];
    },
    extendMap(){
      console.log("MapSize :"+this.corners.length);
      const dist_between_corners = 100; // Offset from each edge
      this.increaseSize(dist_between_corners);
      const offset = 50;
      this.svgWidth += 100;
      this.corners.push({ id: 'top-left'+this.corners.length, x: 0 + offset  , y: 0 + offset , repr_x: -1 , repr_y: 1});
      this.corners.push({ id: 'bottom-left'+this.corners.length, x: 0 + offset  , y: 0 + 200 - offset, repr_x: -1 , repr_y: 0});
    },
    increaseSize(offset = 100){
      this.svgWidth += 100;
      this.corners.forEach(corner => {
        corner.x += offset;
      });
      this.edges.forEach(edge => {
        edge.x1 += offset;
        edge.x2 += offset;
      });
      this.position.x -= offset;
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
</style>