<!-- src/components/Sidebar.vue -->
<template>
    <div class="sidebar" >
        <p class="active">CurrentValues:</p>
        <p class="x_val" >X: {{x_rounded}} </p>
        <p class="y_val" >Y: {{y_rounded}} </p>
        <p class="z_val" >Z: {{z_rounded}} </p>
        <svg width="100%" height="50%" viewBox="0 0 100 100">
          <line  key="bar" x1="50%" y1="0%" x2="50%" y2="100%" stroke="black" stroke-width="2%" />
          <line key="barTop" x1="30%" y1="0%" x2="70%" y2="0%" stroke="black" stroke-width="2%" />
          <line key="barBottom" x1="30%" y1="100%" x2="70%" y2="100%" stroke="black" stroke-width="2%" />
          <line key="barMiddle" x1="40%" y1="50%" x2="60%" y2="50%" stroke="black" stroke-width="1%" />
          <polygon :points="arrowHeadPoints" style="fill:lime;stroke:black;stroke-width:2" />
        </svg>
        <div>
          <div class="display">StepSize: <span id="stepSizeDisplay">{{stepSize}}</span></div>
          <input type="range" id="stepSizeSlider" class="slider" min="0.001" max="0.35" step="0.001" :value="stepSize" @input="updateStepSize">
       </div>
      </div>
  </template>
  
  <script>
  export default {
    name: 'SidebarDisplay',
    props: {
        x: Number, 
        y: Number,
        z: Number,
    },
    data() {
      return {
            x_rounded: this.x,
            y_rounded: this.y,
            z_rounded: this.z,
            arrowY: 0,
            arrowHeadPoints: "50,50 75,60 75,40",
            stepSize: 0.05
        }
    },
    watch: {
        x: function (newVal) {
            this.x_rounded = newVal.toFixed(3);
        },
        y: function (newVal) {
            this.y_rounded = newVal.toFixed(3);
        },
        z: function (newVal) {
            this.z_rounded = newVal.toFixed(3);
            const hundredPercent = 10.0;
            this.arrowY = (( 100.0 * newVal ) / hundredPercent )* (0.5 );
            this.arrowHeadPoints = `51,${50 - this.arrowY} 75,${60 - this.arrowY} 75,${40 - this.arrowY}`;
        }
    },
    methods: {
        updateStepSize(event) {
            const newStepSize = event.target.value;
            this.stepSize = Number(newStepSize);
            this.$emit('update-stepSize', this.stepSize);
        }
    },

    
  }
  </script>
  
  <style scoped>
/* The side navigation menu */
div.sidebar  {
    user-select: none;
    left: 0%;
    top: 10%;
    width: 80%;
    height: 80%!important;
    margin: 0;
    padding: 0;
    width: 200px;
    background-color: #f1f1f1;
    position: absolute;
    height: 100%;
    overflow: auto;
  }
  .display {
    padding: 10px;
    text-align: center;
    color: black;
  }
  
  /* Sidebar links */
  .sidebar p {
    display: block;
    color: black;
    padding: 16px;
    text-decoration: none;
  }
  
  #stepSizeSlider {
    width: 80%;
    position: relative;
    left: 10%!important;
  }
  /* Active/current link */
  .sidebar p.active {
    background-color: #04AA6D;
    color: white;
  }
  
  /* Links on mouse-over */
  .sidebar p:hover:not(.active) {
    background-color: #555;
    color: white;
  }
  
  /* Page content. The value of the margin-left property should match the value of the sidebar's width property */
  div.content {
    margin-left: 200px;
    padding: 1px 16px;
    height: 1000px;
  }
  
  /* On screens that are less than 700px wide, make the sidebar into a topbar */
  @media screen and (max-width: 700px) {
    .sidebar {
      width: 100%;
      height: auto;
      position: relative;
    }
    .sidebar p {float: left;}
    div.content {margin-left: 0;}
  }
  
  /* On screens that are less than 400px, display the bar vertically, instead of horizontally */
  @media screen and (max-width: 400px) {
    .sidebar p {
      text-align: center;
      float: none;
    }
  }
  </style>
  