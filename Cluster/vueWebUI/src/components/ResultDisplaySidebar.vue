<!-- src/components/Sidebar.vue -->
<template>
    <div class="sidebar">
        <p class="active">CurrentValues:</p>
        <p class="x_val" >X: {{x_rounded}} </p>
        <p class="y_val" >Y: {{y_rounded}} </p>
        <p class="z_val" >Z: {{z_rounded}} </p>
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
        }
    }

    
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
  
  /* Sidebar links */
  .sidebar p {
    display: block;
    color: black;
    padding: 16px;
    text-decoration: none;
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
  