<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElTable, ElTableColumn, ElTag } from 'element-plus'
import { adminApi } from '@/api/modules'
import type { DashboardData } from '@/types'
import EChart from '@/components/EChart.vue'
import { fmtNum } from '@/utils/format'

const data = ref<DashboardData | null>(null)
const loading = ref(true)
onMounted(async () => {
  try { data.value = await adminApi.dashboard() } finally { loading.value = false }
})

const axisColor = '#8a93a6'
const splitColor = 'rgba(138,147,166,0.15)'
const line = (title: string, color: string, key: 'publishTrend' | 'viewTrend') => ({
  backgroundColor: 'transparent',
  title: { text: title, textStyle: { fontSize: 13, color: axisColor }, left: 6, top: 4 },
  tooltip: { trigger: 'axis' },
  grid: { left: 12, right: 16, top: 42, bottom: 8, containLabel: true },
  xAxis: { type: 'category', data: (data.value?.[key] ?? []).map((x) => x.date.slice(5)), axisLabel: { color: axisColor, fontSize: 10 }, axisLine: { lineStyle: { color: splitColor } } },
  yAxis: { type: 'value', minInterval: 1, splitLine: { lineStyle: { color: splitColor } }, axisLabel: { color: axisColor } },
  series: [{ type: 'line', smooth: true, symbol: 'none', data: (data.value?.[key] ?? []).map((x) => x.count), lineStyle: { color, width: 2.5 }, areaStyle: { color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [{ offset: 0, color: color + '55' }, { offset: 1, color: color + '00' }] } } }]
})
</script>

<template>
  <div>
    <div v-if="loading" class="grid"><div v-for="i in 3" :key="i" class="skeleton" style="height: 300px"></div></div>
    <template v-else-if="data">
      <div class="grid">
        <div class="card panel"><EChart :option="line('近 30 天发文量', '#FF5A1F', 'publishTrend')" height="260px" /></div>
        <div class="card panel"><EChart :option="line('近 30 天浏览量', '#38BDF8', 'viewTrend')" height="260px" /></div>
        <div class="card panel">
          <EChart :option="{
            backgroundColor: 'transparent',
            title: { text: '分类占比', textStyle: { fontSize: 13, color: axisColor }, left: 6, top: 4 },
            tooltip: { trigger: 'item' },
            legend: { bottom: 0, textStyle: { color: axisColor, fontSize: 11 } },
            series: [{ type: 'pie', radius: ['40%', '66%'], center: ['50%', '46%'], data: data.categoryShare, label: { show: false }, color: ['#FF5A1F', '#38BDF8', '#F59E0B', '#34D399', '#A78BFA'] }]
          }" height="260px" />
        </div>
      </div>
      <div class="card panel" style="margin-top: 18px">
        <h3 class="panel-title">浏览 TOP10</h3>
        <el-table :data="data.top10" size="small" stripe>
          <el-table-column type="index" label="#" width="50" />
          <el-table-column prop="title" label="标题" min-width="260" show-overflow-tooltip />
          <el-table-column prop="categoryName" label="分类" width="110" />
          <el-table-column label="浏览量" width="110" align="right">
            <template #default="{ row }">{{ fmtNum(row.viewCount) }}</template>
          </el-table-column>
        </el-table>
      </div>
    </template>
  </div>
</template>

<style scoped>
.grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; }
.panel { padding: 14px; }
.panel-title { margin: 4px 0 12px; font-size: 15px; color: var(--el-text-color-primary); }
@media (max-width: 1199px) { .grid { grid-template-columns: 1fr; } }
</style>