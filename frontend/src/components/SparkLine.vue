<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{ values: number[]; height?: number }>(), { height: 180 })
const W = 320

const points = computed(() => {
  const vs = props.values
  if (vs.length < 2) return ''
  const max = Math.max(...vs)
  const min = Math.min(...vs)
  const span = max - min || 1
  return vs.map((v, i) => `${((i / (vs.length - 1)) * W).toFixed(1)},${(8 + (1 - (v - min) / span) * (props.height - 24)).toFixed(1)}`).join(' ')
})
const area = computed(() => (points.value ? `0,${props.height} ${points.value} ${W},${props.height}` : ''))
</script>

<template>
  <svg :viewBox="`0 0 ${W} ${height}`" class="spark" preserveAspectRatio="none" role="img" aria-label="市场规模趋势（演示数据）">
    <defs>
      <linearGradient id="sparkFill" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#FF5A1F" stop-opacity="0.35" />
        <stop offset="1" stop-color="#FF5A1F" stop-opacity="0" />
      </linearGradient>
    </defs>
    <polygon :points="area" fill="url(#sparkFill)" />
    <polyline :points="points" fill="none" stroke="#FF5A1F" stroke-width="2.5" stroke-linejoin="round" stroke-linecap="round" />
  </svg>
</template>

<style scoped>
.spark { width: 100%; display: block; }
</style>