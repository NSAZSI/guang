<script setup lang="ts">
withDefaults(defineProps<{ text?: string }>(), { text: '暂无内容' })
</script>

<template>
  <div class="empty">
    <svg width="72" height="72" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2" opacity="0.5">
      <path d="M4 22h16a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2H8a2 2 0 0 0-2 2v16a2 2 0 0 1-2 2zm0 0a2 2 0 0 1-2-2v-9c0-1.1.9-2 2-2h2"/>
      <path d="M18 14h-8M15 18h-5M10 6h8v4h-8z"/>
    </svg>
    <p>{{ text }}</p>
  </div>
</template>

<style scoped>
.empty { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 56px 0; color: var(--muted); }
.empty p { margin: 0; font-size: 14px; }
</style>