<script setup lang="ts">
import { ref } from 'vue'

// 项目介绍文档是独立的静态 HTML（public/docs），自带目录、阅读进度与回到顶部，
// 因此以占满剩余视口的 iframe 嵌入，让它在自己的窗口内滚动，这些功能才能正常工作。
const src = '/docs/adpulse-intro.html'
const loading = ref(true)
</script>

<template>
  <div class="intro">
    <div v-if="loading" class="intro-loading muted">正在加载项目介绍文档…</div>
    <iframe :src="src" title="AdPulse 项目介绍" class="intro-frame" @load="loading = false"></iframe>
    <a :href="src" target="_blank" rel="noopener" class="open-full" title="在新标签页中全屏阅读">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"><path d="M14 3h7v7M10 14 21 3M21 14v7H3V3h7"/></svg>
      全屏阅读
    </a>
  </div>
</template>

<style scoped>
/* 102px = 固定导航 64px + 快讯条 38px */
.intro { position: relative; height: calc(100vh - 102px); height: calc(100dvh - 102px); }
.intro-frame { display: block; width: 100%; height: 100%; border: 0; background: var(--bg); }
.intro-loading { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; font-size: 14px; }
.open-full {
  position: absolute; left: 16px; bottom: 16px; z-index: 2; display: inline-flex; align-items: center; gap: 6px;
  font-size: 12px; padding: 7px 12px; border-radius: 999px; color: var(--text);
  background: var(--glass); backdrop-filter: blur(10px); -webkit-backdrop-filter: blur(10px); border: 1px solid var(--line);
  transition: color 0.2s, border-color 0.2s;
}
.open-full:hover { color: var(--accent); border-color: var(--accent); }
</style>
