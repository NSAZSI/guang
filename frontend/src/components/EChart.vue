<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, shallowRef, watch } from 'vue'
import echarts from './echarts'
import type { EChartsType, EChartsCoreOption } from 'echarts/core'
import { useThemeStore } from '@/stores/theme'

const props = withDefaults(defineProps<{ option: EChartsCoreOption; height?: string }>(), { height: '320px' })
const el = ref<HTMLDivElement>()
const chart = shallowRef<EChartsType>()
const themeStore = useThemeStore()
let observer: ResizeObserver | null = null

function init() {
  if (!el.value) return
  chart.value?.dispose()
  chart.value = echarts.init(el.value, themeStore.theme === 'dark' ? 'dark' : undefined)
  chart.value.setOption(props.option)
}

onMounted(() => {
  init()
  observer = new ResizeObserver(() => chart.value?.resize())
  if (el.value) observer.observe(el.value)
})

watch(() => props.option, (v) => chart.value?.setOption(v), { deep: true })
watch(() => themeStore.theme, init)

onBeforeUnmount(() => {
  observer?.disconnect()
  chart.value?.dispose()
})
</script>

<template>
  <div ref="el" :style="{ height, width: '100%' }"></div>
</template>