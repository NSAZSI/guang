import { get, post, put, patch, del } from './http'
import type {
  AdminNewsDetail, AdminNewsItem, BannerAdmin, Category, DashboardData, HomeData,
  LoginResult, LogItem, NewsDetail, NewsItem, Paged, RelatedItem, SaveNewsPayload,
  StatsData, TagItem, UserInfo
} from '@/types'

export const authApi = {
  login: (userName: string, password: string) => post<LoginResult>('/auth/login', { userName, password }),
  me: () => get<UserInfo>('/auth/me')
}

export const portalApi = {
  home: () => get<HomeData>('/portal/home'),
  list: (params: { categoryId?: number; keyword?: string; page?: number; pageSize?: number; sort?: string; tagId?: number }) =>
    get<Paged<NewsItem>>('/portal/news', params as Record<string, unknown>),
  detail: (id: number) => get<NewsDetail>(`/portal/news/${id}`),
  related: (id: number) => get<RelatedItem[]>(`/portal/news/${id}/related`),
  hot: () => get<NewsItem[]>('/portal/hot'),
  categories: () => get<Category[]>('/portal/categories'),
  stats: () => get<StatsData>('/portal/stats')
}

export const adminApi = {
  newsList: (params: { status?: string; categoryId?: number; keyword?: string; page?: number; pageSize?: number }) =>
    get<Paged<AdminNewsItem>>('/admin/news', params as Record<string, unknown>),
  newsDetail: (id: number) => get<AdminNewsDetail>(`/admin/news/${id}`),
  newsCreate: (data: SaveNewsPayload) => post<{ id: number }>('/admin/news', data),
  newsUpdate: (id: number, data: SaveNewsPayload) => put<void>(`/admin/news/${id}`, data),
  newsStatus: (id: number, action: string) => patch<void>(`/admin/news/${id}/status`, { action }),
  newsRemove: (id: number) => del<void>(`/admin/news/${id}`),
  aiSummary: (id: number) => post<{ aiStatus: string }>(`/admin/news/${id}/ai-summary`),

  categories: () => get<Category[]>('/admin/categories'),
  categoryCreate: (data: { name: string; slug: string; sortOrder: number }) => post<{ id: number }>('/admin/categories', data),
  categoryUpdate: (id: number, data: { name: string; slug: string; sortOrder: number }) => put<void>(`/admin/categories/${id}`, data),
  categoryRemove: (id: number) => del<void>(`/admin/categories/${id}`),

  tags: () => get<TagItem[]>('/admin/tags'),
  tagCreate: (name: string) => post<{ id: number }>('/admin/tags', { name }),
  tagUpdate: (id: number, name: string) => put<void>(`/admin/tags/${id}`, { name }),
  tagRemove: (id: number) => del<void>(`/admin/tags/${id}`),

  banners: () => get<BannerAdmin[]>('/admin/banners'),
  bannerCreate: (data: { imageUrl: string; linkNewsId: number | null; sortOrder: number; isEnabled: boolean }) => post<{ id: number }>('/admin/banners', data),
  bannerUpdate: (id: number, data: { imageUrl: string; linkNewsId: number | null; sortOrder: number; isEnabled: boolean }) => put<void>(`/admin/banners/${id}`, data),
  bannerRemove: (id: number) => del<void>(`/admin/banners/${id}`),

  upload: (file: File) => {
    const fd = new FormData()
    fd.append('file', file)
    return post<{ url: string }>('/admin/upload', fd)
  },
  dashboard: () => get<DashboardData>('/admin/dashboard'),
  logs: (params: { page?: number; pageSize?: number }) => get<Paged<LogItem>>('/admin/logs', params as Record<string, unknown>)
}