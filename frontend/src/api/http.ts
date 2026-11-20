import axios from 'axios'
import { ElMessage } from 'element-plus'

export const TOKEN_KEY = 'adpulse_token'
export const USER_KEY = 'adpulse_user'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || '/api',
  timeout: 20000
})

http.interceptors.request.use((cfg) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) cfg.headers.Authorization = `Bearer ${token}`
  return cfg
})

function handleUnauthorized(message?: string) {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
  if (location.pathname.startsWith('/admin') && location.pathname !== '/admin/login') {
    ElMessage.warning(message || '登录已过期，请重新登录')
    location.href = '/admin/login'
  }
}

http.interceptors.response.use(
  (res) => {
    const body = res.data
    if (body && typeof body === 'object' && 'code' in body) {
      if (body.code === 0) return body.data
      if (body.code === 40100) {
        handleUnauthorized(body.message)
        return Promise.reject(new Error(body.message))
      }
      ElMessage.error(body.message || '请求失败')
      return Promise.reject(new Error(body.message))
    }
    return body
  },
  (err) => {
    const status = err.response?.status
    const body = err.response?.data
    if (status === 401) handleUnauthorized(body?.message)
    else ElMessage.error(body?.message || '网络异常，请稍后重试')
    return Promise.reject(err)
  }
)

export default http
export const get = <T>(url: string, params?: Record<string, unknown>) => http.get(url, { params }) as unknown as Promise<T>
export const post = <T>(url: string, data?: unknown) => http.post(url, data) as unknown as Promise<T>
export const put = <T>(url: string, data?: unknown) => http.put(url, data) as unknown as Promise<T>
export const patch = <T>(url: string, data?: unknown) => http.patch(url, data) as unknown as Promise<T>
export const del = <T>(url: string) => http.delete(url) as unknown as Promise<T>