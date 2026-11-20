<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

interface FlashItem { id: number; title: string; time: string }

const router = useRouter()
const items = ref<FlashItem[]>([])
const current = ref(0)
const connected = ref(false)
let es: EventSource | null = null
let watchdog = 0
let cycle = 0
let lastEvent = Date.now()

const url = (import.meta.env.VITE_API_BASE || '/api') + '/sse/newsflash'

function connect() {
  es?.close()
  es = new EventSource(url)
  es.addEventListener('hello', () => { connected.value = true; lastEvent = Date.now() })
  es.addEventListener('ping', () => { lastEvent = Date.now() })
  es.addEventListener('news', (e) => {
    lastEvent = Date.now()
    try {
      const d = JSON.parse((e as MessageEvent).data) as FlashItem
      items.value = [d, ...items.value].slice(0, 10)
      current.value = 0
    } catch { /* 忽略异常帧 */ }
  })
  es.onerror = () => { connected.value = false } // EventSource 自带重连，watchdog 兜底
}

onMounted(() => {
  connect()
  watchdog = window.setInterval(() => {
    if (Date.now() - lastEvent > 45000) { connect(); lastEvent = Date.now() }
  }, 10000)
  cycle = window.setInterval(() => {
    if (!document.hidden && items.value.length > 1) current.value = (current.value + 1) % items.value.length
  }, 6000)
  // 调试/验收钩子
  ;(window as unknown as Record<string, unknown>).__adpulseFlash = {
    get lastEvent() { return lastEvent },
    get count() { return items.value.length },
    reconnect: connect
  }
})

onBeforeUnmount(() => {
  es?.close()
  clearInterval(watchdog)
  clearInterval(cycle)
})

const open = (id: number) => router.push(`/news/${id}`)
</script>

<template>
  <div class="flash">
    <span class="flash-label"><span class="dot" :class="{ off: !connected }"></span> 实时快讯</span>
    <div v-if="items.length" class="flash-body">
      <transition name="flash-slide" mode="out-in">
        <button :key="items[current].id + '-' + current" class="flash-item" @click="open(items[current].id)">
          <span class="just">刚刚发布</span>
          <span class="flash-title">{{ items[current].title }}</span>
        </button>
      </transition>
    </div>
    <span v-else class="flash-idle muted">{{ connected ? '通道已连接，等待最新发布…' : '正在连接快讯通道…' }}</span>
    <span v-if="items.length" class="flash-count muted">{{ items.length }} 条</span>
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
.flash-title { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; transition: color 0.2s; }
.flash-idle { flex: 1; font-size: 12px; }
.flash-count { flex: none; font-size: 11px; }
.flash-slide-enter-active, .flash-slide-leave-active { transition: all 0.3s ease; }
.flash-slide-enter-from { opacity: 0; transform: translateY(10px); }
.flash-slide-leave-to { opacity: 0; transform: translateY(-10px); }
@media (max-width: 767px) { .flash { padding: 0 16px; } .flash-count { display: none; } }
</style>