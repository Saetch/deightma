<script setup>
import SVGMap from './components/SVGMap.vue'


</script>


<template>
  <body>
    
    <div class="grid-container">

      <div class="wrapper">
        <SVGMap msg="You did it!" />
      </div>


    </div>

  </body>


</template>

<style scoped>
header {
  line-height: 1.5;
}

.grid-container {
  display: grid;
  grid-template-columns: 1fr;
  grid-template-rows: auto 1fr;
  gap: var(--section-gap);
  padding: var(--section-gap);
}

@media (min-width: 1024px) {
  header {
    display: flex;
    place-items: center;
    padding-right: calc(var(--section-gap) / 2);
  }

  body .wrapper {
    place-items: flex-start;
    flex-wrap: wrap;
  }
}
</style>
