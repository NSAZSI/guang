<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElInput, ElPagination } from 'element-plus'
import { portalApi } from '@/api/modules'
import type { Category, NewsItem, Paged } from '@/types'
import NewsCard from '@/components/NewsCard.vue'
import SkeletonList from '@/components/SkeletonList.vue'
import EmptyState from '@/components/EmptyState.vue'

const route = useRoute()
const router = useRouter()
const categories = ref<Category[]>([])
const data = ref<Paged<NewsItem> | null>(null)
const loading = ref(true)
const searchInput = ref('')

const categoryId = computed(() => Number(route.query.category) || 0)
const keyword = computed(() => String(route.query.keyword || ''))
const tag = computed(() => String(route.query.tag || ''))
const sort = computed(() => String(route.query.sort || 'time'))
const page = computed(() => Number(route.query.page) || 1)

function updateQuery(patch: Record<string, string | number | undefined>) {
  const q: Record<string, string> = {}
  const merged: Record<string, unknown> = { category: categoryId.value || undefined, keyword: keyword.value || undefined, tag: tag.value || undefined, sort: sort.value !== 'time' ? sort.value : undefined, page: page.value > 1 ? page.value : undefined, ...patch }
  for (const [k, v] of Object.entries(merged)) if (v !== undefined && v !== '' && v !== 0) q[k] = String(v)
  router.replace({ query: q })
}

async function load() {
  loading.value = true
  searchInput.value = keyword.value
  try {
    data.value = await portalApi.list({
      categoryId: categoryId.value || undefined,
      keyword: keyword.value || undefined,
      tag: tag.value || undefined,
      page: page.value,
      pageSize: 10,
      sort: sort.value
    })
  } finally {
    loading.value = false
  }
}

watch(() => route.query, load, { immediate: true })
portalApi.categories().then((c) => (categories.value = c))

const doSearch = () => updateQuery({ keyword: searchInput.value.trim() || undefined, page: undefined })
</script>

<template>
  <div class="list-page container">
    <header class="page-head">
      <h1>新闻情报</h1>
      <p class="muted">广告行业全量动态 · 支持分类筛选与关键词检索</p>
    </header>

    <div class="filters card">
      <div class="pills">
        <button :class="{ on: categoryId === 0 }" @click="updateQuery({ category: undefined, page: undefined })">全部</button>
        <button v-for="c in categories" :key="c.id" :class="{ on: categoryId === c.id }" @click="updateQuery({ category: c.id, page: undefined })">{{ c.name }}</button>
      </div>
      <div class="tools">
        <el-input v-model="searchInput" placeholder="搜索标题 / 摘要关键词" clearable class="search" @keyup.enter="doSearch" @clear="doSearch">
          <template #append>
            <button class="search-btn" @click="doSearch">搜索</button>
          </template>
        </el-input>
        <div class="sort">
          <button :class="{ on: sort === 'time' }" @click="updateQuery({ sort: undefined, page: undefined })">最新</button>
          <button :class="{ on: sort === 'hot' }" @click="updateQuery({ sort: 'hot', page: undefined })">最热</button>
        </div>
      </div>
    </div>

    <div v-if="tag" class="result-info">
      <span class="tag-filter-chip">✦ 标签：{{ tag }}
        <button class="tag-x" aria-label="清除标签筛选" @click="updateQuery({ tag: undefined, page: undefined })">✕</button>
      </span>
      <span class="muted">共 {{ data?.total ?? 0 }} 条相关新闻</span>
    </div>

    <div v-if="keyword" class="result-info muted">
      搜索 “<span class="accent-text">{{ keyword }}</span>” 共 {{ data?.total ?? 0 }} 条结果
    </div>

    <SkeletonList v-if="loading" :rows="6" />
    <template v-else-if="data && data.items.length">
      <TransitionGroup name="stagger" tag="div" class="cards">
        <NewsCard v-for="n in data.items" :key="n.id" :item="n" :keyword="keyword" />
      </TransitionGroup>
      <div class="pager">
        <el-pagination background layout="total, prev, pager, next" :total="data.total" :page-size="data.pageSize" :current-page="page" @current-change="(p: number) => updateQuery({ page: p > 1 ? p : undefined })" />
      </div>
    </template>
    <EmptyState v-else text="没有找到相关新闻，换个关键词试试" />
  </div>
</template>

<style scoped>
.list-page { padding-top: 28px; }
.page-head h1 { margin: 0 0 6px; font-size: 30px; font-weight: 800; color: var(--text-strong); }
.page-head p { margin: 0 0 20px; font-size: 14px; }
.filters { padding: 16px 18px; display: flex; flex-direction: column; gap: 14px; margin-bottom: 20px; }
.pills { display: flex; gap: 8px; overflow-x: auto; padding-bottom: 2px; }
.pills button { flex: none; padding: 7px 16px; border-radius: 999px; border: 1px solid var(--line); background: transparent; color: var(--muted); font-size: 13px; cursor: pointer; transition: all 0.2s; }
.pills button.on { background: var(--accent); border-color: var(--accent); color: #fff; font-weight: 600; }
.tools { display: flex; gap: 12px; align-items: center; }
.search { flex: 1; }
.search-btn { border: none; background: var(--accent); color: #fff; padding: 0 18px; cursor: pointer; font-size: 13px; height: 100%; }
.sort { display: flex; border: 1px solid var(--line); border-radius: 9px; overflow: hidden; flex: none; }
.sort button { padding: 8px 16px; border: none; background: transparent; color: var(--muted); font-size: 13px; cursor: pointer; }
.sort button.on { background: var(--accent-soft); color: var(--accent); font-weight: 600; }
.result-info { margin-bottom: 14px; font-size: 13px; }
.tag-filter-chip { display: inline-flex; align-items: center; gap: 6px; padding: 4px 12px; margin-right: 10px; border-radius: 999px; background: rgba(255, 90, 31, 0.12); border: 1px solid rgba(255, 90, 31, 0.4); color: var(--accent); font-size: 13px; }
.tag-x { background: none; border: none; color: inherit; cursor: pointer; font-size: 12px; padding: 0 2px; }
.tag-x:hover { color: var(--text); }
.cards { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; }
.pager { display: flex; justify-content: center; margin-top: 28px; }
@media (max-width: 767px) {
  .page-head h1 { font-size: 24px; }
  .tools { flex-direction: column; align-items: stretch; }
  .sort { align-self: flex-start; }
  .cards { grid-template-columns: 1fr; }
}
</style>