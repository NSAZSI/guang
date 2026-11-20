import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test'

const BASE = 'http://localhost:5173'
const API = 'http://localhost:5000'

async function adminApi(): Promise<{ api: APIRequestContext; token: string }> {
  const api = await pwRequest.newContext({ baseURL: API })
  const r = await api.post('/api/auth/login', { data: { userName: 'admin', password: 'AdPulse@2026' } })
  return { api, token: (await r.json()).data.token }
}

// 运行前提：后端以 Ai__ApiKey=wrong-key-on-purpose 启动（mock 服务拒绝该密钥 → 401）
test('错误密钥路径：文章照常发布 + Failed 规则兜底 + 前端诚实标注', async ({ page }) => {
  test.setTimeout(90000)
  const { api, token } = await adminApi()
  const auth = { Authorization: `Bearer ${token}` }
  const title = `AI故障验收${Date.now().toString().slice(-6)}：密钥错误不影响发文主流程`
  const r = await api.post('/api/admin/news', {
    headers: auth,
    data: {
      title, summary: 'AI 故障演练演示稿。',
      content: '<p>广告行业观察：媒介投放成本上行，品牌方更重视转化效率与数据合规。</p>',
      coverUrl: '/covers/cover-07.svg', categoryId: 4, tagIds: [5],
      publish: true, isTop: false, isHeadline: false
    }
  })
  expect(r.ok()).toBeTruthy() // 发布接口本身绝不被 AI 拖垮
  const id = (await r.json()).data.id

  let d: any
  for (let i = 0; i < 40; i++) {
    d = (await (await api.get(`/api/admin/news/${id}`, { headers: auth })).json()).data
    if (d.aiStatus !== 'Pending') break
    await page.waitForTimeout(1000)
  }
  expect(d.status).toBe('Published')
  expect(d.aiStatus).toBe('Failed')
  expect(d.aiSummary.length).toBeGreaterThan(0) // 规则兜底摘要仍在

  await page.goto(`${BASE}/news/${id}`)
  await expect(page.locator('.ai-card')).toContainText('规则兜底', { timeout: 10000 })
  await expect(page.locator('.fallback-note')).toContainText('AI 服务暂不可用')
  await page.screenshot({ path: __dirname + '/screenshots/ai-failed.png' })

  await api.delete(`/api/admin/news/${id}`, { headers: auth })
  await api.dispose()
})