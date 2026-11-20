<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { get } from '@/api/http'

const health = ref<{ status: string; database: boolean; time: string } | null>(null)
const error = ref('')

onMounted(async () => {
  try {
    health.value = await get('/health')
  } catch (e) {
    error.value = String(e)
  }
})
</script>

<template>
  <div class="shell">
    <header class="brand">
      <span class="pulse"></span>
      <h1>AdPulse · 广告脉搏</h1>
    </header>
    <div class="card status">
      <template v-if="health">
        <div class="ok">● 骨架已就绪</div>
        <p>后端状态：{{ health.status }} ｜ 数据库连接：{{ health.database ? '正常' : '异常' }}</p>
        <p class="muted">服务器时间（UTC）：{{ health.time }}</p>
      </template>
      <template v-else-if="error">
        <div class="fail">● 后端未连通</div>
        <p class="muted">{{ error }}</p>
      </template>
      <div v-else class="skeleton" style="height: 80px"></div>
    </div>
  </div>
</template>

<style scoped>
.shell { min-height: 100vh; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 28px; padding: 24px; }
.brand { display: flex; align-items: center; gap: 12px; }
.brand h1 { font-size: 28px; letter-spacing: 1px; color: var(--text-strong); margin: 0; }
.pulse { width: 12px; height: 12px; border-radius: 50%; background: var(--accent); box-shadow: 0 0 0 0 var(--accent-soft); animation: pulse 1.8s infinite; }
@keyframes pulse { 0% { box-shadow: 0 0 0 0 rgba(255, 90, 31, 0.5); } 70% { box-shadow: 0 0 0 14px rgba(255, 90, 31, 0); } 100% { box-shadow: 0 0 0 0 rgba(255, 90, 31, 0); } }
.status { padding: 28px 36px; min-width: 320px; text-align: center; }
.ok { color: #34d399; font-weight: 700; font-size: 18px; margin-bottom: 10px; }
.fail { color: var(--accent); font-weight: 700; font-size: 18px; margin-bottom: 10px; }
</style>