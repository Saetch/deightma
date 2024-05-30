<script setup>
import HelloWorld from './components/HelloWorld.vue'
</script>

<template>
  <body>

    <div class="wrapper">
      <HelloWorld msg="You did it!" />
    </div>
  </body>


</template>

<style scoped>
header {
  line-height: 1.5;
}


@media (min-width: 1024px) {
  header {
    display: flex;
    place-items: center;
    padding-right: calc(var(--section-gap) / 2);
  }



  body .wrapper {
    display: flex;
    place-items: flex-start;
    flex-wrap: wrap;
  }
}
</style>
