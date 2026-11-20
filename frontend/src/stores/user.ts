import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { authApi } from '@/api/modules'
import { TOKEN_KEY, USER_KEY } from '@/api/http'
import type { UserInfo } from '@/types'

export const useUserStore = defineStore('user', () => {
  const token = ref(localStorage.getItem(TOKEN_KEY) || '')
  const user = ref<UserInfo | null>(JSON.parse(localStorage.getItem(USER_KEY) || 'null'))

  const isAdmin = computed(() => user.value?.role === 'Admin')
  const loggedIn = computed(() => !!token.value)

  async function login(userName: string, password: string) {
    const r = await authApi.login(userName, password)
    token.value = r.token
    user.value = r.user
    localStorage.setItem(TOKEN_KEY, r.token)
    localStorage.setItem(USER_KEY, JSON.stringify(r.user))
  }

  async function fetchMe() {
    user.value = await authApi.me()
    localStorage.setItem(USER_KEY, JSON.stringify(user.value))
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
  }

  return { token, user, isAdmin, loggedIn, login, fetchMe, logout }
})