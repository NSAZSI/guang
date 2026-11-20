<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { portalApi } from '@/api/modules'
import type { HomeData, StatsData } from '@/types'
import NewsCard from '@/components/NewsCard.vue'
import SkeletonList from '@/components/SkeletonList.vue'
import CountUp from '@/components/CountUp.vue'
import EChart from '@/components/EChart.vue'
import { fmtNum } from '@/utils/format'

const home = ref<HomeData | null>(null)
const stats = ref<StatsData | null>(null)
const loading = ref(true)
const activeCat = ref(0)

const slides = computed(() => home.value?.banners ?? [])
const idx = ref(0)
let timer = 0
const go = (i: number) => { idx.value = (i + slides.value.length) % Math.max(1, slides.value.length) }
const next = () => go(idx.value + 1)
let touchX = 0
const onTouchStart = (e: TouchEvent) => { touchX = e.touches[0].clientX }
const onTouchEnd = (e: TouchEvent) => {
  const dx = e.changedTouches[0].clientX - touchX
  if (dx > 40) go(idx.value - 1)
  else if (dx < -40) next()
}

const feeds = computed(() => home.value?.categoryFeeds ?? [])
const currentItems = computed(() => feeds.value.find((f) => f.categoryId === activeCat.value)?.items ?? [])

const miniOption = computed(() => ({
  backgroundColor: 'transparent',
  grid: { left: 8, right: 8, top: 14, bottom: 8, containLabel: true },
  tooltip: { trigger: 'axis' },
  xAxis: { type: 'category', data: stats.value?.marketTrend.months ?? [], axisLabel: { show: false }, axisTick: { show: false }, axisLine: { lineStyle: { color: 'rgba(138,147,166,0.3)' } } },
  yAxis: { type: 'value', splitLine: { lineStyle: { color: 'rgba(138,147,166,0.15)' } }, axisLabel: { color: '#8a93a6', fontSize: 10 } },
  series: [{ type: 'line', smooth: true, symbol: 'none', data: stats.value?.marketTrend.values ?? [], lineStyle: { color: '#FF5A1F', width: 2.5 }, areaStyle: { color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [{ offset: 0, color: 'rgba(255,90,31,0.35)' }, { offset: 1, color: 'rgba(255,90,31,0)' }] } } }]
}))

onMounted(async () => {
  try {
    const [h, s] = await Promise.all([portalApi.home(), portalApi.stats()])
    home.value = h
    stats.value = s
    activeCat.value = h.categoryFeeds[0]?.categoryId ?? 0
  } finally {
    loading.value = false
  }
  timer = window.setInterval(next, 5000)
})
onBeforeUnmount(() => clearInterval(timer))
</script>

<template>
  <div class="home container">
    <!-- 头条轮播 -->
    <section v-if="!loading && slides.length" class="hero card" @touchstart="onTouchStart" @touchend="onTouchEnd">
      <div class="hero-track" :style="{ transform: `translateX(-${idx * 100}%)` }">
        <router-link v-for="b in slides" :key="b.id" :to="b.linkNewsId ? `/news/${b.linkNewsId}` : '/news'" class="slide">
          <img :src="b.imageUrl" :alt="b.linkNewsTitle || '头条'" />
          <div class="slide-mask"></div>
          <div class="slide-text">
            <span class="tag-chip">头条</span>
            <h2>{{ b.linkNewsTitle }}</h2>
          </div>
        </router-link>
      </div>
      <div class="dots">
        <button v-for="(b, i) in slides" :key="b.id" :class="{ on: i === idx }" :aria-label="`第${i + 1}张`" @click="go(i)"></button>
      </div>
    </section>
    <div v-else-if="loading" class="skeleton hero-sk"></div>

    <!-- 统计数字 -->
    <section v-if="home" class="counters">
      <div class="card counter"><div class="num accent-text"><CountUp :end="home.counters.newsCount" /></div><div class="muted label">收录新闻</div></div>
      <div class="card counter"><div class="num accent-text"><CountUp :end="home.counters.totalViews" /></div><div class="muted label">累计浏览</div></div>
      <div class="card counter"><div class="num accent-text"><CountUp :end="home.counters.categoryCount" /></div><div class="muted label">栏目分类</div></div>
    </section>

    <div class="grid">
      <!-- 分类新闻流 -->
      <section class="feed">
        <div class="tabs">
          <button v-for="f in feeds" :key="f.categoryId" :class="{ on: f.categoryId === activeCat }" @click="activeCat = f.categoryId">{{ f.categoryName }}</button>
        </div>
        <SkeletonList v-if="loading" :rows="4" />
        <TransitionGroup v-else name="stagger" tag="div" class="cards">
          <NewsCard v-for="n in currentItems" :key="n.id" :item="n" />
        </TransitionGroup>
        <router-link to="/news" class="more muted">查看全部新闻 →</router-link>
      </section>

      <!-- 侧栏 -->
      <aside class="side">
        <div class="card panel">
          <h3 class="panel-title">热榜 TOP10</h3>
          <ol v-if="home" class="hot">
            <li v-for="(n, i) in home.hot" :key="n.id">
              <router-link :to="`/news/${n.id}`">
                <span class="rank" :class="{ top3: i < 3 }">{{ i + 1 }}</span>
                <span class="hot-title">{{ n.title }}</span>
                <span class="muted hot-views">{{ fmtNum(n.viewCount) }}</span>
              </router-link>
            </li>
          </ol>
        </div>
        <div class="card panel">
          <h3 class="panel-title">行业数据速览 <span class="muted demo-note">演示数据</span></h3>
          <EChart v-if="stats" :option="miniOption" height="180px" />
          <router-link to="/data" class="btn-accent" style="margin-top: 12px">查看完整数据</router-link>
        </div>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.home { padding-top: 24px; }
.hero { position: relative; overflow: hidden; border-radius: 18px; }
.hero-sk { height: 380px; border-radius: 18px; }
.hero-track { display: flex; transition: transform 0.55s cubic-bezier(0.22, 1, 0.36, 1); }
.slide { position: relative; flex: 0 0 100%; display: block; }
.slide img { width: 100%; height: 380px; object-fit: cover; }
.slide-mask { position: absolute; inset: 0; background: linear-gradient(180deg, transparent 40%, rgba(5, 7, 12, 0.85)); }
.slide-text { position: absolute; left: 28px; right: 28px; bottom: 26px; display: flex; flex-direction: column; gap: 10px; }
.slide-text h2 { margin: 0; color: #f5f7fa; font-size: 26px; line-height: 1.4; font-weight: 800; }
.dots { position: absolute; right: 22px; top: 18px; display: flex; gap: 7px; }
.dots button { width: 8px; height: 8px; border-radius: 999px; border: none; background: rgba(255, 255, 255, 0.4); cursor: pointer; transition: all 0.25s; padding: 0; }
.dots button.on { width: 22px; background: var(--accent); }
.counters { display: grid; grid-template-columns: repeat(3, 1fr); gap: 18px; margin: 22px 0; }
.counter { padding: 20px 24px; text-align: center; }
.num { font-size: 30px; font-weight: 800; font-variant-numeric: tabular-nums; }
.label { font-size: 13px; margin-top: 4px; }
.grid { display: grid; grid-template-columns: 1fr 340px; gap: 24px; align-items: start; }
.tabs { display: flex; gap: 8px; margin-bottom: 18px; overflow-x: auto; padding-bottom: 4px; }
.tabs button { flex: none; padding: 8px 18px; border-radius: 999px; border: 1px solid var(--line); background: transparent; color: var(--muted); font-size: 14px; cursor: pointer; transition: all 0.2s; }
.tabs button.on { background: var(--accent); border-color: var(--accent); color: #fff; font-weight: 600; }
.cards { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; }
.more { display: inline-block; margin-top: 18px; font-size: 14px; }
.more:hover { color: var(--accent); }
.side { display: flex; flex-direction: column; gap: 20px; }
.panel { padding: 20px; }
.panel-title { margin: 0 0 14px; font-size: 16px; color: var(--text-strong); display: flex; align-items: center; gap: 8px; }
.panel-title::before { content: ''; width: 4px; height: 16px; background: var(--accent); border-radius: 2px; }
.demo-note { font-size: 11px; font-weight: 400; margin-left: auto; }
.hot { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 12px; }
.hot a { display: flex; align-items: baseline; gap: 10px; font-size: 13px; }
.hot a:hover .hot-title { color: var(--accent); }
.rank { flex: none; width: 20px; font-weight: 800; font-style: italic; color: var(--muted); font-size: 15px; }
.rank.top3 { color: var(--accent); }
.hot-title { flex: 1; line-height: 1.5; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; transition: color 0.2s; }
.hot-views { flex: none; font-size: 11px; }
@media (max-width: 1199px) { .grid { grid-template-columns: 1fr; } .side { flex-direction: row; } .side .panel { flex: 1; } }
@media (max-width: 767px) {
  .slide img { height: 240px; }
  .hero-sk { height: 240px; }
  .slide-text h2 { font-size: 18px; }
  .slide-text { left: 16px; right: 16px; bottom: 16px; }
  .counters { gap: 10px; }
  .counter { padding: 14px 8px; }
  .num { font-size: 20px; }
  .cards { grid-template-columns: 1fr; }
  .side { flex-direction: column; }
}
</style>