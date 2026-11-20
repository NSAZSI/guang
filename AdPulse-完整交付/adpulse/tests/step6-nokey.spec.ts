import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test'

const BASE = 'http://localhost:5173'
const API = 'http://localhost:5000'

async function adminApi(): Promise<{ api: APIRequestContext; token: string }> {
  const api = await pwRequest.newContext({ baseURL: API })
  const r = await api.post('/api/auth/login', { data: { userName: 'admin', password: 'AdPulse@2026' } })
  return { api, token: (await r.json()).data.token }
}

async function publish(api: APIRequestContext, token: string, title: string): Promise<number> {
  const r = await api.post('/api/admin/news', {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      title, summary: 'AI 验收演示稿。',
      content: '<p>广告主正在加大程序化广告预算，短视频与直播电商库存成为争夺焦点。品牌营销人员更关注 ROI 与转化数据。</p><p>业内人士认为，智能投放与内容种草将成为下季度的主旋律，媒介代理公司加速转型。</p>',
      coverUrl: '/covers/cover-03.svg', categoryId: 2, tagIds: [3],
      publish: true, isTop: false, isHeadline: false
    }
  })
  expect(r.ok()).toBeTruthy()
  return (await r.json()).data.id
}

test('无密钥路径：规则兜底摘要 + 标签自动入库 + 标签点击筛选', async ({ page }) => {
  test.setTimeout(60000)
  const { api, token } = await adminApi()
  const auth = { Authorization: `Bearer ${token}` }
  const title = `AI降级验收${Date.now().toString().slice(-6)}：程序化广告预算流向短视频库存`
  const id = await publish(api, token, title)

  let d: any
  for (let i = 0; i < 20; i++) {
    d = (await (await api.get(`/api/admin/news/${id}`, { headers: auth })).json()).data
    if (d.aiStatus !== 'Pending') break
    await page.waitForTimeout(500)
  }
  expect(d.aiStatus).toBe('Fallback')

  const portal = (await (await api.get(`/api/portal/news/${id}`)).json()).data
  expect(portal.aiSummary.length).toBeGreaterThan(0)
  expect(portal.aiSummary.length).toBeLessThanOrEqual(81)
  expect(portal.aiTags.length).toBeGreaterThanOrEqual(3)
  for (const t of portal.aiTags) expect(portal.tags).toContain(t) // 标签真实入库合并

  await page.goto(`${BASE}/news/${id}`)
  await expect(page.locator('.ai-card')).toContainText('规则生成摘要', { timeout: 10000 })
  await expect(page.locator('.fallback-note')).toContainText('未配置 AI 密钥')
  await page.screenshot({ path: __dirname + '/screenshots/ai-fallback.png' })

  // 点击 AI 标签 → 列表页精确筛出这篇新闻
  const firstTag = portal.aiTags[0]
  await page.locator('.tag-chip.ai').first().click()
  await expect(page).toHaveURL(/\/news\?tag=/, { timeout: 10000 })
  await expect(page.locator('.tag-filter-chip')).toContainText(firstTag)
  await expect(page.locator('.cards')).toContainText(title, { timeout: 10000 })
  await page.screenshot({ path: __dirname + '/screenshots/ai-tag-filter.png' })

  await api.delete(`/api/admin/news/${id}`, { headers: auth })
  await api.dispose()
})