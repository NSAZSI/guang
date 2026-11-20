<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { portalApi } from '@/api/modules'
import type { StatsData } from '@/types'
import EChart from '@/components/EChart.vue'

const stats = ref<StatsData | null>(null)
const loading = ref(true)

onMounted(async () => {
  try { stats.value = await portalApi.stats() } finally { loading.value = false }
})

const axisColor = '#8a93a6'
const splitColor = 'rgba(138,147,166,0.15)'

const trendOption = computed(() => ({
  backgroundColor: 'transparent',
  title: { text: `广告市场规模趋势（${stats.value?.marketTrend.unit ?? ''}）`, textStyle: { fontSize: 13, color: axisColor }, left: 6, top: 4 },
  tooltip: { trigger: 'axis' },
  grid: { left: 12, right: 16, top: 44, bottom: 10, containLabel: true },
  xAxis: { type: 'category', data: stats.value?.marketTrend.months ?? [], axisLabel: { color: axisColor, fontSize: 10 }, axisLine: { lineStyle: { color: splitColor } } },
  yAxis: { type: 'value', splitLine: { lineStyle: { color: splitColor } }, axisLabel: { color: axisColor } },
  series: [{ type: 'line', smooth: true, data: stats.value?.marketTrend.values ?? [], symbolSize: 6, lineStyle: { color: '#FF5A1F', width: 3 }, itemStyle: { color: '#FF5A1F' }, areaStyle: { color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [{ offset: 0, color: 'rgba(255,90,31,0.32)' }, { offset: 1, color: 'rgba(255,90,31,0)' }] } } }]
}))

const shareOption = computed(() => ({
  backgroundColor: 'transparent',
  title: { text: '媒介投放份额（%）', textStyle: { fontSize: 13, color: axisColor }, left: 6, top: 4 },
  tooltip: { trigger: 'item', formatter: '{b}: {c}%' },
  legend: { bottom: 0, textStyle: { color: axisColor, fontSize: 11 }, itemWidth: 12, itemHeight: 8 },
  series: [{ type: 'pie', radius: ['42%', '68%'], center: ['50%', '46%'], data: stats.value?.mediaShare ?? [], label: { show: false }, itemStyle: { borderColor: 'rgba(0,0,0,0.2)', borderWidth: 2 }, color: ['#FF5A1F', '#38BDF8', '#F59E0B', '#34D399', '#A78BFA', '#F472B6', '#8A93A6'] }]
}))

const categoryOption = computed(() => ({
  backgroundColor: 'transparent',
  title: { text: '品类投放对比（亿元）', textStyle: { fontSize: 13, color: axisColor }, left: 6, top: 4 },
  tooltip: { trigger: 'axis' },
  grid: { left: 12, right: 16, top: 44, bottom: 10, containLabel: true },
  xAxis: { type: 'category', data: (stats.value?.categoryCompare ?? []).map((x) => x.name), axisLabel: { color: axisColor, fontSize: 10, interval: 0, rotate: 24 }, axisLine: { lineStyle: { color: splitColor } } },
  yAxis: { type: 'value', splitLine: { lineStyle: { color: splitColor } }, axisLabel: { color: axisColor } },
  series: [{ type: 'bar', data: (stats.value?.categoryCompare ?? []).map((x) => x.value), barWidth: 22, itemStyle: { borderRadius: [6, 6, 0, 0], color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [{ offset: 0, color: '#FF5A1F' }, { offset: 1, color: 'rgba(255,90,31,0.25)' }] } } }]
}))

const regionOption = computed(() => ({
  backgroundColor: 'transparent',
  title: { text: '地区分布（%）', textStyle: { fontSize: 13, color: axisColor }, left: 6, top: 4 },
  tooltip: { trigger: 'axis' },
  grid: { left: 12, right: 30, top: 44, bottom: 10, containLabel: true },
  xAxis: { type: 'value', splitLine: { lineStyle: { color: splitColor } }, axisLabel: { color: axisColor } },
  yAxis: { type: 'category', inverse: true, data: (stats.value?.regionDistribution ?? []).map((x) => x.name), axisLabel: { color: axisColor }, axisLine: { lineStyle: { color: splitColor } } },
  series: [{ type: 'bar', data: (stats.value?.regionDistribution ?? []).map((x) => x.value), barWidth: 14, label: { show: true, position: 'right', color: axisColor, fontSize: 11 }, itemStyle: { borderRadius: [0, 6, 6, 0], color: '#38BDF8' } }]
}))
</script>

<template>
  <div class="data-page container">
    <header class="page-head">
      <h1>行业数据</h1>
      <p class="muted">广告市场全景视图 · 四组核心图表</p>
    </header>

    <div class="note card">
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><path d="M12 16v-4M12 8h.01"/></svg>
      {{ stats?.note || '行业数据为演示样例，仅用于作品展示。' }}
    </div>

    <div v-if="loading" class="grid">
      <div v-for="i in 4" :key="i" class="skeleton" style="height: 360px"></div>
    </div>
    <div v-else class="grid">
      <div class="card chart-card"><EChart :option="trendOption" height="320px" /></div>
      <div class="card chart-card"><EChart :option="shareOption" height="320px" /></div>
      <div class="card chart-card"><EChart :option="categoryOption" height="320px" /></div>
      <div class="card chart-card"><EChart :option="regionOption" height="320px" /></div>
    </div>
  </div>
</template>

<style scoped>
.data-page { padding-top: 28px; }
.page-head h1 { margin: 0 0 6px; font-size: 30px; font-weight: 800; color: var(--text-strong); }
.page-head p { margin: 0 0 18px; font-size: 14px; }
.note { display: flex; align-items: center; gap: 10px; padding: 12px 16px; font-size: 13px; color: var(--muted); margin-bottom: 20px; border-left: 3px solid var(--accent); }
.grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; }
.chart-card { padding: 14px; }
@media (max-width: 767px) {
  .page-head h1 { font-size: 24px; }
  .grid { grid-template-columns: 1fr; }
}
</style>