export interface UserInfo { id: number; userName: string; displayName: string; role: 'Admin' | 'Editor' }
export interface LoginResult { token: string; expiresAt: string; user: UserInfo }

export interface NewsItem {
  id: number; title: string; summary: string; coverUrl: string
  categoryId: number; categoryName: string; tags: string[]
  viewCount: number; isTop: boolean; publishedAt: string | null
}

export interface NewsDetail {
  id: number; title: string; summary: string; content: string; coverUrl: string
  categoryId: number; categoryName: string; tags: string[]
  viewCount: number; isTop: boolean; publishedAt: string | null; authorName: string
  aiSummary: string | null; aiStatus: string; aiTags: string[]
  prevId: number | null; prevTitle: string | null; nextId: number | null; nextTitle: string | null
}

export interface RelatedItem { id: number; title: string; coverUrl: string; publishedAt: string | null }
export interface BannerItem { id: number; imageUrl: string; linkNewsId: number | null; linkNewsTitle: string | null }
export interface CategoryFeed { categoryId: number; categoryName: string; items: NewsItem[] }
export interface SiteCounters { newsCount: number; totalViews: number; categoryCount: number }
export interface HomeData {
  banners: BannerItem[]; headlines: NewsItem[]; categoryFeeds: CategoryFeed[]
  hot: NewsItem[]; counters: SiteCounters
}
export interface Category { id: number; name: string; slug: string; sortOrder: number }
export interface Paged<T> { total: number; page: number; pageSize: number; items: T[] }

export interface AdminNewsItem {
  id: number; title: string; coverUrl: string; categoryId: number; categoryName: string
  status: 'Draft' | 'Published' | 'Recycled'; isTop: boolean; isHeadline: boolean
  viewCount: number; authorId: number; authorName: string; aiStatus: string
  publishedAt: string | null; updatedAt: string
}
export interface AdminNewsDetail {
  id: number; title: string; summary: string; content: string; coverUrl: string
  categoryId: number; tagIds: number[]; status: string; isTop: boolean; isHeadline: boolean
  aiStatus: string; aiSummary: string | null
}
export interface SaveNewsPayload {
  title: string; summary: string; content: string; coverUrl: string; categoryId: number
  tagIds: number[]; publish: boolean; isTop: boolean; isHeadline: boolean
}
export interface TagItem { id: number; name: string; usage: number }
export interface BannerAdmin {
  id: number; imageUrl: string; linkNewsId: number | null; linkNewsTitle: string | null
  sortOrder: number; isEnabled: boolean
}
export interface TrendPoint { date: string; count: number }
export interface NameValue { name: string; value: number }
export interface DashboardData {
  publishTrend: TrendPoint[]; viewTrend: TrendPoint[]; categoryShare: NameValue[]; top10: NewsItem[]
}
export interface LogItem { id: number; userName: string; action: string; target: string; detail: string; createdAt: string }
export interface StatsData {
  note: string
  marketTrend: { months: string[]; values: number[]; unit: string }
  mediaShare: NameValue[]; categoryCompare: NameValue[]; regionDistribution: NameValue[]
}