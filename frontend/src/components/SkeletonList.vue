<script setup lang="ts">
withDefaults(defineProps<{ rows?: number }>(), { rows: 4 })
</script>

<template>
  <div class="sk-list">
    <div v-for="i in rows" :key="i" class="card sk-card">
      <div class="skeleton sk-cover"></div>
      <div class="sk-body">
        <div class="skeleton" style="height: 18px; width: 80%"></div>
        <div class="skeleton" style="height: 13px; width: 100%"></div>
        <div class="skeleton" style="height: 13px; width: 60%"></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.sk-list { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; }
.sk-card { overflow: hidden; }
.sk-cover { aspect-ratio: 16 / 9; border-radius: 0; }
.sk-body { padding: 16px; display: flex; flex-direction: column; gap: 10px; }
@media (max-width: 767px) { .sk-list { grid-template-columns: 1fr; } }
</style>