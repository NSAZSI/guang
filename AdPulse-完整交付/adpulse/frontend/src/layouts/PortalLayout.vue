<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { ElDrawer } from 'element-plus'
import { useThemeStore } from '@/stores/theme'
import NewsFlash from '@/components/NewsFlash.vue'

const theme = useThemeStore()
const route = useRoute()
const drawer = ref(false)
const links = [
  { to: '/', label: '首页' },
  { to: '/news', label: '新闻' },
  { to: '/data', label: '行业数据' },
  { to: '/intro', label: '项目介绍' }
]
const isActive = (to: string) => (to === '/' ? route.path === '/' : route.path.startsWith(to))
</script>

<template>
  <div class="portal">
    <header class="nav">
      <div class="container nav-inner">
        <router-link to="/" class="logo">
          <span class="pulse"></span>
          <span class="logo-text">AdPulse <em>广告脉搏</em></span>
        </router-link>
        <nav class="links desktop">
          <router-link v-for="l in links" :key="l.to" :to="l.to" class="link" :class="{ active: isActive(l.to) }">{{ l.label }}</router-link>
        </nav>
        <div class="actions">
          <button class="icon-btn" :title="theme.theme === 'dark' ? '切换亮色' : '切换暗色'" @click="theme.toggle()">
            <svg v-if="theme.theme === 'dark'" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M1 12h2M21 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4"/></svg>
            <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"/></svg>
          </button>
          <router-link to="/admin" class="admin-link desktop">管理入口</router-link>
          <button class="icon-btn mobile-menu" aria-label="菜单" @click="drawer = true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 6h18M3 12h18M3 18h18"/></svg>
          </button>
        </div>
      </div>
      <NewsFlash />
    </header>

    <el-drawer v-model="drawer" direction="rtl" size="72%" :with-header="false">
      <div class="drawer-body">
        <div class="drawer-brand"><span class="pulse"></span> AdPulse 广告脉搏</div>
        <router-link v-for="l in links" :key="l.to" :to="l.to" class="drawer-link" :class="{ active: isActive(l.to) }" @click="drawer = false">{{ l.label }}</router-link>
        <router-link to="/admin" class="drawer-link" @click="drawer = false">管理入口</router-link>
        <button class="btn-accent" style="margin-top: 18px" @click="theme.toggle()">切换{{ theme.theme === 'dark' ? '亮色' : '暗色' }}主题</button>
      </div>
    </el-drawer>

    <main class="main" :class="{ immersive: route.meta.immersive }">
      <router-view v-slot="{ Component }">
        <transition name="fade" mode="out-in">
          <component :is="Component" />
        </transition>
      </router-view>
    </main>

    <footer v-if="!route.meta.immersive" class="footer">
      <div class="container">
        <div class="foot-brand"><span class="pulse"></span> AdPulse · 广告脉搏</div>
        <p class="muted">面试演示作品 · 所有新闻与行业数据均为演示样例，不代表真实事件与统计</p>
        <p class="muted tech">Vue 3.5 · Vite 8 · ASP.NET Core 10 · EF Core · SQLite · SSE</p>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.nav { position: fixed; top: 0; left: 0; right: 0; z-index: 100; background: var(--glass); backdrop-filter: blur(14px); -webkit-backdrop-filter: blur(14px); border-bottom: 1px solid var(--line); }
.nav-inner { display: flex; align-items: center; gap: 28px; height: 64px; }
.logo { display: flex; align-items: center; gap: 10px; font-weight: 800; font-size: 18px; color: var(--text-strong); letter-spacing: 0.5px; }
.logo em { font-style: normal; font-weight: 500; font-size: 14px; color: var(--muted); margin-left: 2px; }
.pulse { width: 10px; height: 10px; border-radius: 50%; background: var(--accent); animation: pulse 1.8s infinite; flex: none; }
@keyframes pulse { 0% { box-shadow: 0 0 0 0 rgba(255, 90, 31, 0.5); } 70% { box-shadow: 0 0 0 10px rgba(255, 90, 31, 0); } 100% { box-shadow: 0 0 0 0 rgba(255, 90, 31, 0); } }
.links { display: flex; gap: 6px; flex: 1; }
.link { padding: 8px 14px; border-radius: 8px; font-size: 14px; color: var(--muted); transition: color 0.2s, background 0.2s; }
.link:hover { color: var(--text-strong); }
.link.active { color: var(--accent); background: var(--accent-soft); font-weight: 600; }
.actions { display: flex; align-items: center; gap: 10px; margin-left: auto; }
.icon-btn { display: inline-flex; align-items: center; justify-content: center; width: 36px; height: 36px; border-radius: 9px; border: 1px solid var(--line); background: transparent; color: var(--text); cursor: pointer; transition: background 0.2s; }
.icon-btn:hover { background: var(--accent-soft); color: var(--accent); }
.admin-link { font-size: 13px; color: var(--muted); border: 1px solid var(--line); padding: 7px 14px; border-radius: 9px; transition: all 0.2s; }
.admin-link:hover { color: var(--accent); border-color: var(--accent); }
.mobile-menu { display: none; }
.main { padding-top: 102px; min-height: calc(100vh - 180px); }
.main.immersive { min-height: 0; }
.footer { margin-top: 64px; border-top: 1px solid var(--line); padding: 30px 0 36px; }
.foot-brand { display: flex; align-items: center; gap: 8px; font-weight: 700; color: var(--text-strong); margin-bottom: 10px; }
.footer p { margin: 4px 0; font-size: 13px; }
.tech { font-size: 12px; opacity: 0.8; }
.drawer-body { display: flex; flex-direction: column; gap: 6px; padding: 8px; }
.drawer-brand { display: flex; align-items: center; gap: 8px; font-weight: 800; font-size: 17px; color: var(--text-strong); padding: 10px 12px 18px; }
.drawer-link { padding: 13px 14px; border-radius: 10px; font-size: 15px; color: var(--text); }
.drawer-link.active { color: var(--accent); background: var(--accent-soft); font-weight: 600; }
@media (max-width: 767px) {
  .desktop { display: none; }
  .mobile-menu { display: inline-flex; }
  .nav-inner { gap: 12px; }
}
</style>