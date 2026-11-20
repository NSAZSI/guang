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
      content: '<p>某头部品牌宣布追加数字广告预算，重点投向信息流与短视频渠道，代理商程序化采购占比提升。</p>',
      coverUrl: '/covers/cover-05.svg', categoryId: 1, tagIds: [1],
      publish: true, isTop: false, isHeadline: false
    }
  })
  expect(r.ok()).toBeTruthy()
  return (await r.json()).data.id
}

async function waitDone(api: APIRequestContext, auth: Record<string, string>, id: number, seconds: number) {
  let d: any
  for (let i = 0; i < seconds; i++) {
    d = (await (await api.get(`/api/admin/news/${id}`, { headers: auth })).json()).data
    if (d.aiStatus === 'Done' || d.aiStatus === 'Failed') return d
    await new Promise(r => setTimeout(r, 1000))
  }
  return d
}

// 运行前提：后端以 Ai__BaseUrl=http://127.0.0.1:5099/v1 Ai__ApiKey=mock-good-key 启动，mock 服务在跑
test('有密钥路径：生成中→AI完成 + 标签合并 + 重新生成换一版', async ({ page }) => {
  test.setTimeout(90000)
  const { api, token } = await adminApi()
  const auth = { Authorization: `Bearer ${token}` }
  const title = `AI生成验收${Date.now().toString().slice(-6)}：头部品牌追加数字广告预算`
  const id = await publish(api, token, title)

  // mock 有 1.5s 延迟：详情页应先显示"生成中"
  await page.goto(`${BASE}/news/${id}`)
  await expect(page.locator('.ai-card')).toContainText('生成中', { timeout: 8000 })

  const d = await waitDone(api, auth, id, 30)
  expect(d.aiStatus).toBe('Done')
  expect(d.aiSummary).toContain('模拟AI摘要')
  const s1 = d.aiSummary

  await page.reload()
  await expect(page.locator('.ai-card')).toContainText('AI 摘要', { timeout: 10000 })
  await expect(page.locator('.ai-card')).not.toContainText('规则生成')
  const portal = (await (await api.get(`/api/portal/news/${id}`)).json()).data
  expect(portal.aiTags.length).toBeGreaterThanOrEqual(3)
  for (const t of portal.aiTags) expect(portal.tags).toContain(t)
  await page.screenshot({ path: __dirname + '/screenshots/ai-done.png' })

  // 重新生成 → 换一版
  const regen = await api.post(`/api/admin/news/${id}/ai-summary`, { headers: auth })
  expect((await regen.json()).data.aiStatus).toBe('Pending')
  const d2 = await waitDone(api, auth, id, 30)
  expect(d2.aiStatus).toBe('Done')
  expect(d2.aiSummary).not.toBe(s1)

  await api.delete(`/api/admin/news/${id}`, { headers: auth })
  await api.dispose()
})