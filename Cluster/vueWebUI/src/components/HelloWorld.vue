<template>
  <div>
    <div
      class="container"
      :style="{ top: position.y + 'px', left: position.x + 'px', transform: 'scale(' + scale + ')' }"
      @mousedown="startDrag"
      @wheel="onWheel"
    >
      <svg width="200" height="200" 
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
      scale: 1,
      additionalCircles: false,
      corners: [],
      edges: []
    };
  },
  mounted() {
    this.calculateCornersAndEdges();
    window.addEventListener('resize', this.calculateCornersAndEdges);
  },
  unmounted() {
    window.removeEventListener('resize', this.calculateCornersAndEdges);
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
        { id: 'top-left', x: this.position.x + offset, y: this.position.y + offset },
        { id: 'top-right', x: this.position.x + 200 - offset, y: this.position.y + offset },
        { id: 'bottom-left', x: this.position.x + offset, y: this.position.y + 200 - offset },
        { id: 'bottom-right', x: this.position.x + 200 - offset, y: this.position.y + 200 - offset },
      ];

      // Calculate edges based on corners
      this.edges = [
        { id: 'top-edge', x1: this.corners[0].x, y1: this.corners[0].y, x2: this.corners[1].x, y2: this.corners[1].y },
        { id: 'right-edge', x1: this.corners[1].x, y1: this.corners[1].y, x2: this.corners[3].x, y2: this.corners[3].y },
        { id: 'bottom-edge', x1: this.corners[3].x, y1: this.corners[3].y, x2: this.corners[2].x, y2: this.corners[2].y },
        { id: 'left-edge', x1: this.corners[2].x, y1: this.corners[2].y, x2: this.corners[0].x, y2: this.corners[0].y },
      ];
    },
  },
};
</script>

<style scoped>
/* center the svg */
.container {
  position: relative;
  width: 200px;
  height: 200px;
  cursor: grab;
  transition: transform 0.1s;
}


.container:active {
  cursor: grabbing;
}
</style>