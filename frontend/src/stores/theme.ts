import { defineStore } from 'pinia'
import { ref } from 'vue'

const KEY = 'adpulse_theme'

export const useThemeStore = defineStore('theme', () => {
  const theme = ref<'dark' | 'light'>((localStorage.getItem(KEY) as 'dark' | 'light') || 'dark')

  function apply() {
    document.documentElement.setAttribute('data-theme', theme.value)
    document.documentElement.classList.toggle('dark', theme.value === 'dark')
  }

  function toggle() {
    theme.value = theme.value === 'dark' ? 'light' : 'dark'
    localStorage.setItem(KEY, theme.value)
    apply()
  }

  apply()
  return { theme, toggle }
})