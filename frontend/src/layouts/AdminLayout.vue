<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElContainer, ElAside, ElHeader, ElMain, ElMenu, ElMenuItem, ElDropdown, ElDropdownMenu, ElDropdownItem, ElDrawer, ElTag, ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'

const user = useUserStore()
const route = useRoute()
const router = useRouter()
const drawer = ref(false)

const menus = [
  { path: '/admin', label: '仪表盘', icon: 'M3 13h8V3H3zM13 21h8V11h-8zM13 3v6h8V3zM3 21h8v-6H3z', admin: false },
  { path: '/admin/news', label: '新闻管理', icon: 'M4 22h16a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2H8a2 2 0 0 0-2 2v16a2 2 0 0 1-2 2zm0 0a2 2 0 0 1-2-2v-9c0-1.1.9-2 2-2h2M18 14h-8M15 18h-5M10 6h8v4h-8z', admin: false },
  { path: '/admin/categories', label: '分类管理', icon: 'M3 7v10a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-6l-2-2H5a2 2 0 0 0-2 2z', admin: true },
  { path: '/admin/tags', label: '标签管理', icon: 'M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82zM7 7h.01', admin: true },
  { path: '/admin/banners', label: '轮播图管理', icon: 'M2 3h20v14H2zM8 21h8M12 17v4', admin: true },
  { path: '/admin/logs', label: '操作日志', icon: 'M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v8h6M16 13H8M16 17H8M10 9H8', admin: true },
  { path: '/admin/recycle', label: '回收站', icon: 'M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2', admin: true }
]
const visibleMenus = computed(() => menus.filter((m) => !m.admin || user.isAdmin))
const active = computed(() => {
  if (route.path.startsWith('/admin/news')) return '/admin/news'
  return route.path
})

onMounted(async () => {
  if (user.token) {
    try { await user.fetchMe() } catch { user.logout(); router.replace('/admin/login') }
  }
})

function logout() {
  user.logout()
  ElMessage.success('已退出登录')
  router.replace('/admin/login')
}
</script>

<template>
  <el-container class="admin-shell">
    <el-aside width="220px" class="aside desktop-aside">
      <div class="brand"><span class="pulse"></span> AdPulse 控制台</div>
      <el-menu :default-active="active" router class="menu">
        <el-menu-item v-for="m in visibleMenus" :key="m.path" :index="m.path">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 8px"><path :d="m.icon"/></svg>
          <span>{{ m.label }}</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-drawer v-model="drawer" direction="ltr" size="240px" :with-header="false">
      <div class="brand" style="padding: 18px 16px"><span class="pulse"></span> AdPulse 控制台</div>
      <el-menu :default-active="active" router @select="drawer = false">
        <el-menu-item v-for="m in visibleMenus" :key="m.path" :index="m.path">{{ m.label }}</el-menu-item>
      </el-menu>
    </el-drawer>

    <el-container>
      <el-header class="header">
        <button class="icon-btn mobile-only" aria-label="菜单" @click="drawer = true">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 6h18M3 12h18M3 18h18"/></svg>
        </button>
        <span class="header-title">{{ visibleMenus.find((m) => m.path === active)?.label || '控制台' }}</span>
        <div class="header-right">
          <router-link to="/" target="_blank" class="view-site">查看前台 ↗</router-link>
          <el-dropdown trigger="click">
            <span class="user-chip">
              {{ user.user?.displayName || '未登录' }}
              <el-tag size="small" :type="user.isAdmin ? 'danger' : 'info'" effect="plain">{{ user.isAdmin ? '管理员' : '编辑' }}</el-tag>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="logout">退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>
      <el-main class="main">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
    </el-container>
  </el-container>
</template>

<style scoped>
.admin-shell { min-height: 100vh; }
.aside { border-right: 1px solid var(--el-border-color-light); background: var(--el-bg-color); }
.brand { display: flex; align-items: center; gap: 8px; font-weight: 800; font-size: 16px; padding: 20px 18px; color: var(--el-text-color-primary); }
.pulse { width: 9px; height: 9px; border-radius: 50%; background: #ff5a1f; animation: pulse 1.8s infinite; }
@keyframes pulse { 0% { box-shadow: 0 0 0 0 rgba(255, 90, 31, 0.5); } 70% { box-shadow: 0 0 0 9px rgba(255, 90, 31, 0); } 100% { box-shadow: 0 0 0 0 rgba(255, 90, 31, 0); } }
.menu { border-right: none; }
.header { display: flex; align-items: center; gap: 14px; border-bottom: 1px solid var(--el-border-color-light); background: var(--el-bg-color); }
.header-title { font-weight: 700; font-size: 16px; color: var(--el-text-color-primary); }
.header-right { margin-left: auto; display: flex; align-items: center; gap: 16px; }
.view-site { font-size: 13px; color: var(--el-text-color-secondary); }
.view-site:hover { color: #ff5a1f; }
.user-chip { display: flex; align-items: center; gap: 8px; cursor: pointer; font-size: 14px; color: var(--el-text-color-primary); }
.main { background: var(--el-fill-color-light); padding: 20px; }
.icon-btn { display: inline-flex; align-items: center; justify-content: center; width: 34px; height: 34px; border-radius: 8px; border: 1px solid var(--el-border-color); background: transparent; color: var(--el-text-color-primary); cursor: pointer; }
.mobile-only { display: none; }
@media (max-width: 767px) {
  .desktop-aside { display: none; }
  .mobile-only { display: inline-flex; }
  .view-site { display: none; }
  .main { padding: 12px; }
}
</style>