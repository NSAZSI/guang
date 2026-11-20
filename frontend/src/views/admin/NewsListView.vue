<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElTable, ElTableColumn, ElTag, ElButton, ElSelect, ElOption, ElInput, ElPagination, ElMessageBox, ElMessage } from 'element-plus'
import { adminApi, portalApi } from '@/api/modules'
import type { AdminNewsItem, Category, Paged } from '@/types'
import { statusLabel, statusType, aiStatusLabel } from '@/utils/news'
import { fmtDateTime, fmtNum } from '@/utils/format'

const router = useRouter()
const data = ref<Paged<AdminNewsItem> | null>(null)
const categories = ref<Category[]>([])
const loading = ref(false)
const query = reactive({ status: '', categoryId: 0, keyword: '', page: 1, pageSize: 10 })

async function load() {
  loading.value = true
  try {
    data.value = await adminApi.newsList({
      status: query.status || undefined,
      categoryId: query.categoryId || undefined,
      keyword: query.keyword || undefined,
      page: query.page, pageSize: query.pageSize
    })
  } finally { loading.value = false }
}

async function act(row: AdminNewsItem, action: string) {
  if (action === 'recycle') {
    await ElMessageBox.confirm(`确定将《${row.title}》移入回收站？`, '二次确认', { type: 'warning', confirmButtonText: '移入回收站', cancelButtonText: '取消' })
  }
  await adminApi.newsStatus(row.id, action)
  ElMessage.success('操作成功')
  load()
}

onMounted(() => { load(); portalApi.categories().then((c) => (categories.value = c)) })
</script>

<template>
  <div>
    <div class="toolbar card">
      <el-select v-model="query.status" placeholder="全部状态" clearable style="width: 130px" @change="query.page = 1; load()">
        <el-option label="已发布" value="Published" /><el-option label="草稿" value="Draft" />
      </el-select>
      <el-select v-model="query.categoryId" placeholder="全部分类" clearable style="width: 130px" @change="query.page = 1; load()">
        <el-option v-for="c in categories" :key="c.id" :label="c.name" :value="c.id" />
      </el-select>
      <el-input v-model="query.keyword" placeholder="搜索标题" clearable style="width: 200px" @keyup.enter="query.page = 1; load()" @clear="query.page = 1; load()" />
      <el-button @click="query.page = 1; load()">查询</el-button>
      <el-button type="primary" class="accent-btn" @click="router.push('/admin/news/create')">+ 写新闻</el-button>
    </div>

    <!-- 桌面表格 -->
    <div class="card table-wrap desktop-block">
      <el-table :data="data?.items ?? []" v-loading="loading" stripe>
        <el-table-column label="标题" min-width="280">
          <template #default="{ row }">
            <div class="title-cell">
              <img :src="row.coverUrl" class="thumb" alt="" />
              <div>
                <div class="t">{{ row.title }} <el-tag v-if="row.isTop" size="small" type="danger" effect="plain">顶</el-tag></div>
                <div class="muted s">{{ row.categoryName }} · {{ row.authorName }}</div>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }"><el-tag :type="statusType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column label="AI" width="100">
          <template #default="{ row }"><span class="muted s">{{ aiStatusLabel(row.aiStatus) }}</span></template>
        </el-table-column>
        <el-table-column label="浏览" width="90" align="right">
          <template #default="{ row }">{{ fmtNum(row.viewCount) }}</template>
        </el-table-column>
        <el-table-column label="更新时间" width="150">
          <template #default="{ row }"><span class="muted s">{{ fmtDateTime(row.updatedAt) }}</span></template>
        </el-table-column>
        <el-table-column label="操作" width="250" fixed="right">
          <template #default="{ row }">
            <el-button size="small" link type="primary" @click="router.push(`/admin/news/${row.id}/edit`)">编辑</el-button>
            <el-button v-if="row.status === 'Draft'" size="small" link type="success" @click="act(row, 'publish')">发布</el-button>
            <el-button v-if="row.status === 'Published'" size="small" link type="warning" @click="act(row, 'unpublish')">撤回</el-button>
            <el-button v-if="row.status === 'Published'" size="small" link @click="act(row, row.isTop ? 'untop' : 'top')">{{ row.isTop ? '取消顶' : '置顶' }}</el-button>
            <el-button size="small" link type="danger" @click="act(row, 'recycle')">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <!-- 手机卡片 -->
    <div class="mobile-block">
      <div v-for="row in data?.items ?? []" :key="row.id" class="card m-card">
        <div class="m-head">
          <img :src="row.coverUrl" class="thumb" alt="" />
          <div class="m-title">{{ row.title }}</div>
        </div>
        <div class="m-meta muted">
          <el-tag :type="statusType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
          <span>{{ row.categoryName }}</span><span>{{ fmtNum(row.viewCount) }} 浏览</span>
        </div>
        <div class="m-actions">
          <el-button size="small" @click="router.push(`/admin/news/${row.id}/edit`)">编辑</el-button>
          <el-button v-if="row.status === 'Draft'" size="small" type="success" @click="act(row, 'publish')">发布</el-button>
          <el-button v-if="row.status === 'Published'" size="small" type="warning" @click="act(row, 'unpublish')">撤回</el-button>
          <el-button v-if="row.status === 'Published'" size="small" @click="act(row, row.isTop ? 'untop' : 'top')">{{ row.isTop ? '取消顶' : '置顶' }}</el-button>
          <el-button size="small" type="danger" @click="act(row, 'recycle')">删除</el-button>
        </div>
      </div>
    </div>

    <div class="pager">
      <el-pagination background layout="total, prev, pager, next" :total="data?.total ?? 0" :page-size="query.pageSize" :current-page="query.page" @current-change="(p: number) => { query.page = p; load() }" />
    </div>
  </div>
</template>

<style scoped>
.toolbar { display: flex; gap: 10px; flex-wrap: wrap; padding: 14px; margin-bottom: 16px; align-items: center; }
.accent-btn { background: #ff5a1f; border-color: #ff5a1f; margin-left: auto; }
.table-wrap { padding: 8px; }
.title-cell { display: flex; gap: 10px; align-items: center; }
.thumb { width: 64px; height: 40px; object-fit: cover; border-radius: 6px; flex: none; }
.t { font-size: 13px; font-weight: 600; color: var(--el-text-color-primary); line-height: 1.4; }
.s { font-size: 12px; }
.muted { color: var(--el-text-color-secondary); }
.pager { display: flex; justify-content: center; margin-top: 16px; }
.mobile-block { display: none; }
.m-card { padding: 14px; margin-bottom: 12px; }
.m-head { display: flex; gap: 10px; }
.m-title { font-size: 14px; font-weight: 600; color: var(--el-text-color-primary); line-height: 1.5; }
.m-meta { display: flex; gap: 10px; align-items: center; font-size: 12px; margin: 10px 0; }
.m-actions { display: flex; gap: 6px; flex-wrap: wrap; }
@media (max-width: 767px) {
  .desktop-block { display: none; }
  .mobile-block { display: block; }
  .accent-btn { margin-left: 0; }
}
</style>