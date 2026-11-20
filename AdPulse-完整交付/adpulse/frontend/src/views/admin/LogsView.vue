<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElTable, ElTableColumn, ElPagination, ElTag } from 'element-plus'
import { adminApi } from '@/api/modules'
import type { LogItem, Paged } from '@/types'
import { fmtDateTime } from '@/utils/format'

const data = ref<Paged<LogItem> | null>(null)
const loading = ref(false)
const page = ref(1)

async function load() {
  loading.value = true
  try { data.value = await adminApi.logs({ page: page.value, pageSize: 15 }) } finally { loading.value = false }
}
onMounted(load)
</script>

<template>
  <div>
    <h3 class="title">操作日志</h3>
    <div class="card" style="padding: 8px">
      <el-table :data="data?.items ?? []" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="70" />
        <el-table-column prop="userName" label="操作人" width="110" />
        <el-table-column label="动作" width="150">
          <template #default="{ row }"><el-tag size="small" effect="plain">{{ row.action }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="target" label="对象" width="110" />
        <el-table-column prop="detail" label="详情" min-width="240" show-overflow-tooltip />
        <el-table-column label="时间" width="160">
          <template #default="{ row }"><span class="muted">{{ fmtDateTime(row.createdAt) }}</span></template>
        </el-table-column>
      </el-table>
    </div>
    <div class="pager">
      <el-pagination background layout="total, prev, pager, next" :total="data?.total ?? 0" :page-size="15" :current-page="page" @current-change="(p: number) => { page = p; load() }" />
    </div>
  </div>
</template>

<style scoped>
.title { margin: 0 0 14px; color: var(--el-text-color-primary); }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.pager { display: flex; justify-content: center; margin-top: 16px; }
</style>