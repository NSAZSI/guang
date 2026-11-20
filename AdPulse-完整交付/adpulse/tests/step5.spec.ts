import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test'

const BASE = 'http://localhost:5173'
const API = 'http://localhost:5000'
const SHOTS = __dirname + '/screenshots'

async function adminApi(): Promise<{ api: APIRequestContext; token: string }> {
  const api = await pwRequest.newContext({ baseURL: API })
  const r = await api.post('/api/auth/login', { data: { userName: 'admin', password: 'AdPulse@2026' } })
  const token = (await r.json()).data.token
  return { api, token }
}

async function publish(api: APIRequestContext, token: string, title: string): Promise<number> {
  const r = await api.post('/api/admin/news', {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      title, summary: 'SSE 验收演示新闻。', content: '<p>SSE 验收正文。</p>',
      coverUrl: '/covers/cover-02.svg', categoryId: 2, tagIds: [3],
      publish: true, isTop: false, isHeadline: false
    }
  })
  return (await r.json()).data.id
}

test('双窗口秒级推送 + 点击跳转 + 撤回不推送 + 连接数统计', async ({ browser }) => {
  const { api, token } = await adminApi()
  const ctxA = await browser.newContext()
  const ctxB = await browser.newContext({ viewport: { width: 375, height: 812 } })
  const a = await ctxA.newPage()
  const b = await ctxB.newPage()
  await a.goto(BASE + '/')
  await b.goto(BASE + '/')
  await expect(a.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })
  await expect(b.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })

  const stats1 = await (await api.get('/api/sse/stats')).json()
  expect(stats1.data.clients).toBeGreaterThanOrEqual(2)

  const title = `SSE快讯验收${Date.now().toString().slice(-6)}：实时推送链路`
  const t0 = Date.now()
  const id = await publish(api, token, title)
  await expect(a.locator('.flash-item')).toContainText(title, { timeout: 5000 })
  await expect(b.locator('.flash-item')).toContainText(title, { timeout: 5000 })
  console.log(`推送延迟约 ${Date.now() - t0}ms（含请求耗时）`)
  await a.screenshot({ path: SHOTS + '/sse-flash.png' })

  // 点击快讯跳详情
  await a.locator('.flash-item').click()
  await expect(a).toHaveURL(new RegExp(`/news/${id}`), { timeout: 10000 })

  // 撤回不触发快讯
  const countBefore = await a.evaluate(() => (window as any).__adpulseFlash.count)
  await api.patch(`/api/admin/news/${id}/status`, { headers: { Authorization: `Bearer ${token}` }, data: { action: 'unpublish' } })
  await a.waitForTimeout(2500)
  const countAfter = await a.evaluate(() => (window as any).__adpulseFlash.count)
  expect(countAfter).toBe(countBefore)

  // 关闭 B 后连接数下降
  await b.close()
  await a.waitForTimeout(2000)
  const stats2 = await (await api.get('/api/sse/stats')).json()
  expect(stats2.data.clients).toBeLessThan(stats1.data.clients)

  await api.delete(`/api/admin/news/${id}`, { headers: { Authorization: `Bearer ${token}` } })
  await ctxA.close(); await ctxB.close(); await api.dispose()
})

test('心跳保活：17 秒内收到 ping 事件', async ({ page }) => {
  await page.goto(BASE + '/')
  await expect(page.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })
  await page.waitForTimeout(17000)
  const last = await page.evaluate(() => (window as any).__adpulseFlash.lastEvent as number)
  expect(Date.now() - last).toBeLessThan(20000)
})

test('断网恢复后自动重连并可再次收到推送', async ({ browser }) => {
  const { api, token } = await adminApi()
  const ctx = await browser.newContext()
  const page = await ctx.newPage()
  await page.goto(BASE + '/')
  await expect(page.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })

  await ctx.setOffline(true)
  await page.waitForTimeout(30000) // 按验收清单：断网 30 秒
  await ctx.setOffline(false)
  await page.waitForTimeout(10000) // EventSource 自动重连窗口

  const title = `SSE重连验收${Date.now().toString().slice(-6)}：断网恢复`
  const id = await publish(api, token, title)
  await expect(page.locator('.flash-item')).toContainText(title, { timeout: 20000 })
  await api.delete(`/api/admin/news/${id}`, { headers: { Authorization: `Bearer ${token}` } })
  await ctx.close(); await api.dispose()
})

test('明暗主题快讯条样式 + 手机端不遮挡正文（截图留证）', async ({ browser }) => {
  const { api, token } = await adminApi()
  const light = await browser.newContext()
  const dark = await browser.newContext({ viewport: { width: 375, height: 812 } })
  await dark.addInitScript(() => localStorage.setItem('adpulse_theme', 'dark'))
  const pl = await light.newPage()
  const pd = await dark.newPage()
  await pl.goto(BASE + '/')
  await pd.goto(BASE + '/')
  await expect(pl.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })
  await expect(pd.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })
  expect(await pd.evaluate(() => document.documentElement.getAttribute('data-theme'))).toBe('dark')

  const title = `SSE主题验收${Date.now().toString().slice(-6)}：双主题快讯`
  const id = await publish(api, token, title)
  await expect(pl.locator('.flash-item')).toContainText(title, { timeout: 5000 })
  await expect(pd.locator('.flash-item')).toContainText(title, { timeout: 5000 })
  await pl.screenshot({ path: SHOTS + '/sse-flash-light.png' })
  await pd.screenshot({ path: SHOTS + '/sse-flash-dark-mobile.png' })

  // 手机端：快讯条底边不得压住正文（header 为 fixed，正文首个子元素才是真实内容起点）
  const flashBox = await pd.locator('.flash').boundingBox()
  const contentBox = await pd.locator('.main > *').first().boundingBox()
  expect(flashBox && contentBox).toBeTruthy()
  expect(flashBox!.y + flashBox!.height).toBeLessThanOrEqual(contentBox!.y + 1)

  await api.delete(`/api/admin/news/${id}`, { headers: { Authorization: `Bearer ${token}` } })
  await light.close(); await dark.close(); await api.dispose()
})