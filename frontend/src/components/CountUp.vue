<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'

const props = withDefaults(defineProps<{ end: number; duration?: number }>(), { duration: 1200 })
const display = ref(0)
let raf = 0

function animate(from: number, to: number) {
  cancelAnimationFrame(raf)
  const start = performance.now()
  const step = (t: number) => {
    const p = Math.min(1, (t - start) / props.duration)
    const eased = 1 - Math.pow(1 - p, 3)
    display.value = Math.round(from + (to - from) * eased)
    if (p < 1) raf = requestAnimationFrame(step)
  }
  raf = requestAnimationFrame(step)
}

onMounted(() => animate(0, props.end))
watch(() => props.end, (v) => animate(display.value, v))
</script>

<template>
  <span>{{ display.toLocaleString('zh-CN') }}</span>
</template>