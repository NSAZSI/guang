<script setup lang="ts">
import type { NewsItem } from '@/types'
import { fmtDate, fmtNum } from '@/utils/format'
import HighlightText from './HighlightText.vue'

defineProps<{ item: NewsItem; keyword?: string }>()
</script>

<template>
  <router-link :to="`/news/${item.id}`" class="card hoverable news-card">
    <div class="cover">
      <img :src="item.coverUrl" :alt="item.title" loading="lazy" />
      <span class="cat">{{ item.categoryName }}</span>
      <span v-if="item.isTop" class="top-badge">置顶</span>
    </div>
    <div class="body">
      <h3><HighlightText :text="item.title" :keyword="keyword" /></h3>
      <p class="summary"><HighlightText :text="item.summary" :keyword="keyword" /></p>
      <div class="meta muted">
        <span>{{ fmtDate(item.publishedAt) }}</span>
        <span class="views">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
          {{ fmtNum(item.viewCount) }}
        </span>
      </div>
    </div>
  </router-link>
</template>

<style scoped>
.news-card { display: flex; flex-direction: column; overflow: hidden; }
.cover { position: relative; aspect-ratio: 16 / 9; overflow: hidden; background: var(--bg-soft); }
.cover img { width: 100%; height: 100%; object-fit: cover; transition: transform 0.4s ease; }
.news-card:hover .cover img { transform: scale(1.05); }
.cat { position: absolute; left: 12px; top: 12px; background: var(--glass); backdrop-filter: blur(8px); color: var(--text); font-size: 12px; padding: 3px 10px; border-radius: 999px; border: 1px solid var(--line); }
.top-badge { position: absolute; right: 12px; top: 12px; background: var(--accent); color: #fff; font-size: 12px; padding: 3px 10px; border-radius: 999px; font-weight: 600; }
.body { padding: 16px 18px 18px; display: flex; flex-direction: column; gap: 8px; flex: 1; }
h3 { margin: 0; font-size: 17px; line-height: 1.5; color: var(--text-strong); display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
.summary { margin: 0; font-size: 13px; color: var(--muted); line-height: 1.7; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
.meta { display: flex; justify-content: space-between; align-items: center; font-size: 12px; margin-top: auto; }
.views { display: inline-flex; align-items: center; gap: 4px; }
</style>