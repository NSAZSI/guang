export function statusLabel(s: string): string {
  return { Draft: '草稿', Published: '已发布', Recycled: '回收站' }[s] ?? s
}
export function statusType(s: string): 'success' | 'info' | 'warning' | 'danger' {
  return ({ Published: 'success', Draft: 'info', Recycled: 'danger' } as const)[s] ?? 'info'
}
export function aiStatusLabel(s: string): string {
  return { None: '未生成', Pending: '生成中', Done: 'AI 已生成', Fallback: '规则生成', Failed: 'AI 异常·已兜底', NotEnabled: '未启用' }[s] ?? s
}