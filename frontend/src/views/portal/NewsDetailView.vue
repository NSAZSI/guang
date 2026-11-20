<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import DOMPurify from 'dompurify'
import { portalApi } from '@/api/modules'
import type { NewsDetail, RelatedItem } from '@/types'
import NewsCard from '@/components/NewsCard.vue'
import EmptyState from '@/components/EmptyState.vue'
import { fmtDateTime, fmtNum } from '@/utils/format'

const route = useRoute()
const router = useRouter()
const detail = ref<NewsDetail | null>(null)
const related = ref<RelatedItem[]>([])
const loading = ref(true)
const notFound = ref(false)
let pollTimer = 0
let pollCount = 0

const safeContent = computed(() => (detail.value ? DOMPurify.sanitize(detail.value.content) : ''))
const aiLabel = computed(() => {
  const s = detail.value?.aiStatus
  if (s === 'Done') return 'AI 摘要'
  if (s === 'Fallback') return '规则生成摘要'
  if (s === 'Pending') return 'AI 摘要生成中…'
  return '编辑摘要'
})
const aiText = computed(() => detail.value?.aiSummary || detail.value?.summary || '')

async function load() {
  const id = Number(route.params.id)
  if (!id) return
  loading.value = true
  notFound.value = false
  clearInterval(pollTimer)
  pollCount = 0
  try {
    const [d, r] = await Promise.all([portalApi.detail(id), portalApi.related(id)])
    detail.value = d
    related.value = r
    if (d.aiStatus === 'Pending') startPolling(id)
  } catch {
    notFound.value = true
  } finally {
    loading.value = false
  }
}

function startPolling(id: number) {
  pollTimer = window.setInterval(async () => {
    pollCount++
    if (pollCount > 20) { clearInterval(pollTimer); return }
    try {
      const d = await portalApi.detail(id)
      detail.value = d
      if (d.aiStatus !== 'Pending') clearInterval(pollTimer)
    } catch { clearInterval(pollTimer) }
  }, 3000)
}

const goTag = (t: string) => router.push({ path: '/news', query: { keyword: t } })

watch(() => route.params.id, load, { immediate: true })
onBeforeUnmount(() => clearInterval(pollTimer))
</script>

<template>
  <div class="detail container">
    <template v-if="loading">
      <div class="skeleton" style="height: 20px; width: 220px; margin-bottom: 22px"></div>
      <div class="skeleton" style="height: 44px; width: 85%; margin-bottom: 16px"></div>
      <div class="skeleton" style="height: 16px; width: 320px; margin-bottom: 30px"></div>
      <div class="skeleton" style="height: 300px"></div>
    </template>

    <EmptyState v-else-if="notFound" text="新闻不存在或已下架" />

    <article v-else-if="detail">
      <nav class="crumb muted">
        <router-link to="/">首页</router-link> /
        <router-link :to="`/news?category=${detail.categoryId}`">{{ detail.categoryName }}</router-link> /
        <span>正文</span>
      </nav>

      <h1 class="title">{{ detail.title }}</h1>
      <div class="meta muted">
        <span>{{ detail.authorName }}</span>
        <span>{{ fmtDateTime(detail.publishedAt) }}</span>
        <span>{{ fmtNum(detail.viewCount) }} 次浏览</span>
        <span v-if="detail.isTop" class="tag-chip">置顶</span>
      </div>

      <div class="ai-card card" :class="{ pending: detail.aiStatus === 'Pending' }">
        <div class="ai-head">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2l2.4 7.2L22 12l-7.6 2.8L12 22l-2.4-7.2L2 12l7.6-2.8z"/></svg>
          <span>{{ aiLabel }}</span>
          <span v-if="detail.aiStatus === 'Fallback'" class="muted fallback-note">未配置 AI 密钥，由规则引擎抽取</span>
        </div>
        <p>{{ aiText }}</p>
      </div>

      <div class="rich-content" v-html="safeContent"></div>

      <div class="tags">
        <span v-for="t in detail.aiTags" :key="'ai-' + t" class="tag-chip ai" @click="goTag(t)">✦ {{ t }}</span>
        <span v-for="t in detail.tags" :key="t" class="tag-chip" @click="goTag(t)">{{ t }}</span>
      </div>

      <div class="prev-next">
        <router-link v-if="detail.prevId" :to="`/news/${detail.prevId}`" class="card pn">
          <span class="muted">上一篇</span><span class="pn-title">{{ detail.prevTitle }}</span>
        </router-link>
        <span v-else></span>
        <router-link v-if="detail.nextId" :to="`/news/${detail.nextId}`" class="card pn right">
          <span class="muted">下一篇</span><span class="pn-title">{{ detail.nextTitle }}</span>
        </router-link>
      </div>

      <section v-if="related.length" class="related">
        <h2 class="rel-title">相关推荐</h2>
        <div class="rel-grid">
          <NewsCard v-for="r in related" :key="r.id" :item="{ id: r.id, title: r.title, summary: '', coverUrl: r.coverUrl, categoryId: detail.categoryId, categoryName: detail.categoryName, tags: [], viewCount: 0, isTop: false, publishedAt: r.publishedAt }" />
        </div>
      </section>
    </article>
  </div>
</template>

<style scoped>
.detail { padding-top: 28px; max-width: 860px; }
.crumb { font-size: 13px; margin-bottom: 18px; display: flex; gap: 6px; flex-wrap: wrap; }
.crumb a:hover { color: var(--accent); }
.title { font-size: 34px; line-height: 1.4; font-weight: 800; color: var(--text-strong); margin: 0 0 16px; letter-spacing: 0.3px; }
.meta { display: flex; gap: 18px; align-items: center; font-size: 13px; flex-wrap: wrap; margin-bottom: 24px; }
.ai-card { padding: 18px 20px; margin-bottom: 28px; border-left: 3px solid var(--accent); }
.ai-card.pending { opacity: 0.75; }
.ai-head { display: flex; align-items: center; gap: 8px; color: var(--accent); font-weight: 700; font-size: 14px; margin-bottom: 8px; }
.fallback-note { font-weight: 400; font-size: 12px; margin-left: auto; }
.ai-card p { margin: 0; line-height: 1.8; font-size: 14px; }
.tags { display: flex; gap: 8px; flex-wrap: wrap; margin: 28px 0; }
.tag-chip.ai { background: var(--accent); color: #fff; }
.prev-next { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin: 30px 0; }
.pn { padding: 16px 18px; display: flex; flex-direction: column; gap: 6px; font-size: 13px; }
.pn:hover { border-color: var(--accent); }
.pn.right { text-align: right; }
.pn-title { color: var(--text-strong); font-weight: 600; line-height: 1.5; display: -webkit-box; -webkit-line-clamp: 1; -webkit-box-orient: vertical; overflow: hidden; }
.rel-title { font-size: 20px; color: var(--text-strong); margin: 36px 0 18px; display: flex; align-items: center; gap: 10px; }
.rel-title::before { content: ''; width: 4px; height: 18px; background: var(--accent); border-radius: 2px; }
.rel-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 18px; }
@media (max-width: 767px) {
  .title { font-size: 24px; }
  .prev-next { grid-template-columns: 1fr; }
  .rel-grid { grid-template-columns: 1fr; }
  .fallback-note { display: none; }
}
</style>