import { createRouter, createWebHistory } from 'vue-router'
import PortalLayout from '@/layouts/PortalLayout.vue'

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
    { path: '/:pathMatch(.*)*', redirect: '/' }
  ]
})

export default router