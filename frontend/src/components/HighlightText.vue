<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{ text: string; keyword?: string }>()

const parts = computed(() => {
  const kw = (props.keyword || '').trim()
  if (!kw) return [{ hit: false, value: props.text }]
  const escaped = kw.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  const re = new RegExp(`(${escaped})`, 'gi')
  return props.text.split(re).filter((s) => s !== '').map((s) => ({ hit: s.toLowerCase() === kw.toLowerCase(), value: s }))
})
</script>

<template>
  <span><template v-for="(p, i) in parts" :key="i"><mark v-if="p.hit" class="hl">{{ p.value }}</mark><template v-else>{{ p.value }}</template></template></span>
</template>