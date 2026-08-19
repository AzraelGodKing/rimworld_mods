<script setup>
defineProps({
  title: { type: String, required: true },
  items: { type: Array, required: true }, // strings
  open: { type: Boolean, default: false },
});
</script>

<template>
  <details class="collapsible" :open="open">
    <summary>{{ title }}</summary>
    <ul>
      <li v-for="item in items" :key="item">{{ item }}</li>
    </ul>
  </details>
</template>
