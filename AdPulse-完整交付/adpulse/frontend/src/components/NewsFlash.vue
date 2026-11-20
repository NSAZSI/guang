<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { portalApi } from '@/api/modules'

interface FlashItem { id: number; title: string; time: string; fresh?: boolean }

const router = useRouter()
const items = ref<FlashItem[]>([])      // SSE 实时推送的新发布
const recent = ref<FlashItem[]>([])     // 进入页面时加载的最近发布，空闲时轮播
const current = ref(0)
const connected = ref(false)
let es: EventSource | null = null
let watchdog = 0
let cycle = 0
let lastEvent = Date.now()
let pings = 0

// 实时推送优先，其后接最近发布（去重）
const shown = computed<FlashItem[]>(() => {
  const pushedIds = new Set(items.value.map((i) => i.id))
  return [...items.value, ...recent.value.filter((r) => !pushedIds.has(r.id))]
})

const url = (import.meta.env.VITE_API_BASE || '/api') + '/sse/newsflash'

function connect() {
  es?.close()
  es = new EventSource(url)
  es.addEventListener('hello', () => { connected.value = true; lastEvent = Date.now() })
  es.addEventListener('ping', () => { pings++; lastEvent = Date.now() })
  es.addEventListener('news', (e) => {
    lastEvent = Date.now()
    try {
      const d = JSON.parse((e as MessageEvent).data) as FlashItem
      items.value = [{ ...d, fresh: true }, ...items.value.filter((i) => i.id !== d.id)].slice(0, 10)
      current.value = 0
    } catch { /* 忽略异常帧 */ }
  })
  es.onerror = () => { connected.value = false } // EventSource 自带重连，watchdog 兜底
}

async function loadRecent() {
  try {
    const page = await portalApi.list({ page: 1, pageSize: 5 })
    recent.value = page.items.map((n) => ({ id: n.id, title: n.title, time: n.publishedAt ?? '' }))
  } catch { /* 最近列表失败不影响实时通道 */ }
}

onMounted(() => {
  connect()
  loadRecent()
  watchdog = window.setInterval(() => {
    if (Date.now() - lastEvent > 45000) { connect(); lastEvent = Date.now() }
  }, 10000)
  cycle = window.setInterval(() => {
    if (!document.hidden && shown.value.length > 1) current.value = (current.value + 1) % shown.value.length
  }, 5000)
  // 调试/验收钩子（count 只统计实时推送条数）
  ;(window as unknown as Record<string, unknown>).__adpulseFlash = {
    get lastEvent() { return lastEvent },
    get count() { return items.value.length },
    get pings() { return pings },
    reconnect: connect
  }
})

onBeforeUnmount(() => {
  es?.close()
  clearInterval(watchdog)
  clearInterval(cycle)
})

const open = (id: number) => router.push(`/news/${id}`)
const active = computed(() => shown.value[Math.min(current.value, shown.value.length - 1)])
</script>

<template>
  <div class="flash">
    <span class="flash-label"><span class="dot" :class="{ off: !connected }"></span> 实时快讯</span>
    <div v-if="active" class="flash-body">
      <transition name="flash-slide" mode="out-in">
        <button :key="active.id + '-' + current" class="flash-item" @click="open(active.id)">
          <span class="just" :class="{ old: !active.fresh }">{{ active.fresh ? '刚刚发布' : '最新' }}</span>
          <span class="flash-title">{{ active.title }}</span>
        </button>
      </transition>
    </div>
    <span v-else class="flash-idle muted">{{ connected ? '通道已连接，等待最新发布…' : '正在连接快讯通道…' }}</span>
    <span v-if="items.length" class="flash-count muted">新 {{ items.length }} 条</span>
  </div>
</template>

<style scoped>
.flash { display: flex; align-items: center; gap: 12px; height: 38px; padding: 0 24px; border-top: 1px solid var(--line); background: var(--bg-soft); font-size: 13px; overflow: hidden; }
.flash-label { display: inline-flex; align-items: center; gap: 6px; color: var(--accent); font-weight: 700; flex: none; font-size: 12px; }
.dot { width: 7px; height: 7px; border-radius: 50%; background: var(--accent); animation: blink 1.6s infinite; }
.dot.off { background: var(--muted); animation: none; }
@keyframes blink { 0%, 100% { opacity: 1; } 50% { opacity: 0.35; } }
.flash-body { flex: 1; min-width: 0; }
.flash-item { display: flex; align-items: center; gap: 8px; width: 100%; border: none; background: transparent; color: var(--text); cursor: pointer; font-size: 13px; padding: 0; text-align: left; }
.flash-item:hover .flash-title { color: var(--accent); }
.just { flex: none; font-size: 11px; color: var(--accent); background: var(--accent-soft); padding: 1px 8px; border-radius: 999px; }
.just.old { color: var(--muted); background: transparent; border: 1px solid var(--line); }
.flash-title { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; transition: color 0.2s; }
.flash-idle { flex: 1; font-size: 12px; }
.flash-count { flex: none; font-size: 11px; }
.flash-slide-enter-active, .flash-slide-leave-active { transition: all 0.3s ease; }
.flash-slide-enter-from { opacity: 0; transform: translateY(10px); }
.flash-slide-leave-to { opacity: 0; transform: translateY(-10px); }
@media (max-width: 767px) { .flash { padding: 0 16px; } .flash-count { display: none; } }
</style>