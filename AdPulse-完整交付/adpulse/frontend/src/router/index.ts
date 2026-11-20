import { createRouter, createWebHistory } from 'vue-router'
import { ElMessage } from 'element-plus'
import PortalLayout from '@/layouts/PortalLayout.vue'
import { useUserStore } from '@/stores/user'

const router = createRouter({
  history: createWebHistory(),
  scrollBehavior: () => ({ top: 0 }),
  routes: [
    {
      path: '/',
      component: PortalLayout,
      children: [
        { path: '', name: 'home', component: () => import('@/views/portal/HomeView.vue') },
        { path: 'news', name: 'news', component: () => import('@/views/portal/NewsListView.vue') },
        { path: 'news/:id(\\d+)', name: 'detail', component: () => import('@/views/portal/NewsDetailView.vue') },
        { path: 'data', name: 'data', component: () => import('@/views/portal/DataView.vue') }
      ]
    },
    { path: '/admin/login', name: 'admin-login', component: () => import('@/views/admin/LoginView.vue') },
    {
      path: '/admin',
      component: () => import('@/layouts/AdminLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', name: 'admin-dashboard', component: () => import('@/views/admin/DashboardView.vue') },
        { path: 'news', name: 'admin-news', component: () => import('@/views/admin/NewsListView.vue') },
        { path: 'news/create', name: 'admin-news-create', component: () => import('@/views/admin/NewsEditView.vue') },
        { path: 'news/:id(\\d+)/edit', name: 'admin-news-edit', component: () => import('@/views/admin/NewsEditView.vue') },
        { path: 'categories', name: 'admin-categories', component: () => import('@/views/admin/CategoriesView.vue'), meta: { adminOnly: true } },
        { path: 'tags', name: 'admin-tags', component: () => import('@/views/admin/TagsView.vue'), meta: { adminOnly: true } },
        { path: 'banners', name: 'admin-banners', component: () => import('@/views/admin/BannersView.vue'), meta: { adminOnly: true } },
        { path: 'logs', name: 'admin-logs', component: () => import('@/views/admin/LogsView.vue'), meta: { adminOnly: true } },
        { path: 'recycle', name: 'admin-recycle', component: () => import('@/views/admin/RecycleView.vue'), meta: { adminOnly: true } }
      ]
    },
    { path: '/:pathMatch(.*)*', redirect: '/' }
  ]
})

router.beforeEach((to) => {
  const user = useUserStore()
  if (to.meta.requiresAuth && !user.token) return { path: '/admin/login', query: { redirect: to.fullPath } }
  if (to.meta.adminOnly && !user.isAdmin) {
    ElMessage.warning('该页面仅管理员可访问')
    return { path: '/admin' }
  }
  if (to.path === '/admin/login' && user.token) return { path: '/admin' }
})

export default router