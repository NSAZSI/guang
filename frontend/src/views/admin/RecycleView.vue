<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElTable, ElTableColumn, ElButton, ElMessage, ElEmpty } from 'element-plus'
import { adminApi } from '@/api/modules'
import type { AdminNewsItem } from '@/types'
import { fmtDateTime } from '@/utils/format'

const list = ref<AdminNewsItem[]>([])
const loading = ref(false)

async function load() {
  loading.value = true
  try {
    const r = await adminApi.newsList({ status: 'Recycled', pageSize: 50 })
    list.value = r.items
  } finally { loading.value = false }
}
onMounted(load)

async function restore(row: AdminNewsItem) {
  await adminApi.newsStatus(row.id, 'restore')
  ElMessage.success(`《${row.title}》已还原为草稿`)
  load()
}
</script>

<template>
  <div>
    <h3 class="title">回收站 <span class="muted">（删除的新闻在此保留，可还原为草稿）</span></h3>
    <div class="card" style="padding: 8px">
      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="title" label="标题" min-width="280" show-overflow-tooltip />
        <el-table-column prop="categoryName" label="分类" width="110" />
        <el-table-column prop="authorName" label="作者" width="110" />
        <el-table-column label="移入时间" width="160">
          <template #default="{ row }"><span class="muted">{{ fmtDateTime(row.updatedAt) }}</span></template>
        </el-table-column>
        <el-table-column label="操作" width="110">
          <template #default="{ row }">
            <el-button size="small" type="success" link @click="restore(row)">还原</el-button>
          </template>
        </el-table-column>
        <template #empty><el-empty description="回收站是空的" /></template>
      </el-table>
    </div>
  </div>
</template>

<style scoped>
.title { margin: 0 0 14px; color: var(--el-text-color-primary); }
.muted { color: var(--el-text-color-secondary); font-size: 12px; font-weight: 400; }
</style>