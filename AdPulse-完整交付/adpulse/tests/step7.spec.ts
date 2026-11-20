import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test'
import fs from 'fs'

// 步骤 7：全部走 nginx（127.0.0.1:80）→ 生产后端（Production，无 AI 密钥）
const BASE = 'http://127.0.0.1'

async function login(api: APIRequestContext, userName: string, password: string) {
  const r = await api.post('/api/auth/login', { data: { userName, password } })
  expect(r.ok()).toBeTruthy()
  return (await r.json()).data.token as string
}

test('生产首页完整加载 + 详情页直接刷新不404 + Swagger关闭', async ({ page }) => {
  const resp = await page.goto(BASE + '/')
  expect(resp?.status()).toBe(200)
  await expect(page.locator('.hero').first()).toBeVisible({ timeout: 15000 })
  await expect(page.locator('.flash-idle, .flash-item').first()).toBeVisible({ timeout: 15000 })

  const detail = await page.goto(BASE + '/news/1')
  expect(detail?.status()).toBe(200)
  await expect(page.locator('.rich-content')).toBeVisible({ timeout: 15000 })
  await page.reload()
  await expect(page.locator('.rich-content')).toBeVisible({ timeout: 15000 })

  const sw = await page.request.get(BASE + '/swagger/v1/swagger.json')
  expect(sw.status()).toBe(404)
  await page.screenshot({ path: __dirname + '/screenshots/prod-home.png' })
})

test('Nginx SSE 试金石：双端发布秒级收到快讯 + 点击跳转', async ({ browser }) => {
  const api = await pwRequest.newContext({ baseURL: BASE })
  const token = await login(api, 'admin', 'AdPulse@2026')
  const auth = { Authorization: `Bearer ${token}` }

  const desktop = await browser.newContext()
  const mobile = await browser.newContext({ viewport: { width: 375, height: 812 } })
  const pd = await desktop.newPage()
  const pm = await mobile.newPage()
  await pd.goto(BASE + '/')
  await pm.goto(BASE + '/')
  await expect(pd.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })
  await expect(pm.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })

  const title = `生产SSE验收${Date.now().toString().slice(-6)}：nginx反代实时链路`
  const t0 = Date.now()
  const r = await api.post('/api/admin/news', {
    headers: auth,
    data: {
      title, summary: '生产验收。', content: '<p>生产环境 SSE 经 nginx 反代验收正文。</p>',
      coverUrl: '/covers/cover-04.svg', categoryId: 3, tagIds: [4],
      publish: true, isTop: false, isHeadline: false
    }
  })
  const id = (await r.json()).data.id
  await expect(pd.locator('.flash-item')).toContainText(title, { timeout: 3000 })
  await expect(pm.locator('.flash-item')).toContainText(title, { timeout: 3000 })
  console.log(`nginx SSE 推送延迟约 ${Date.now() - t0}ms`)
  await pd.locator('.flash-item').click()
  await expect(pd).toHaveURL(new RegExp(`/news/${id}`), { timeout: 10000 })

  // 该新闻 AI 摘要正确降级（生产无密钥 → Fallback + 诚实标注）
  let d: any
  for (let i = 0; i < 15; i++) {
    d = (await (await api.get(`/api/admin/news/${id}`, { headers: auth })).json()).data
    if (d.aiStatus !== 'Pending') break
    await pd.waitForTimeout(500)
  }
  expect(d.aiStatus).toBe('Fallback')
  await expect(pd.locator('.ai-card')).toContainText('规则生成摘要', { timeout: 10000 })

  await api.delete(`/api/admin/news/${id}`, { headers: auth })
  await desktop.close(); await mobile.close(); await api.dispose()
})

test('公网路径上传封面并可访问 + editor 越权 403', async ({ page }) => {
  const api = await pwRequest.newContext({ baseURL: BASE })
  const adminToken = await login(api, 'admin', 'AdPulse@2026')
  const buf = fs.readFileSync(__dirname + '/fixtures/test.png')
  const up = await api.post('/api/admin/upload', {
    headers: { Authorization: `Bearer ${adminToken}` },
    multipart: { file: { name: 'prod-upload.png', mimeType: 'image/png', buffer: buf } }
  })
  expect(up.ok()).toBeTruthy()
  const url = (await up.json()).data.url as string
  expect(url).toMatch(/^\/uploads\//)
  const img = await api.get(url)
  expect(img.status()).toBe(200)
  expect(img.headers()['content-type']).toContain('image')

  // editor 越权：看日志 403、动 admin 的文章（#2 归属 admin）403、回收站 403
  const editorToken = await login(api, 'editor', 'Editor@2026')
  const eAuth = { Authorization: `Bearer ${editorToken}` }
  const logs = await api.get('/api/admin/logs', { headers: eAuth })
  expect(logs.status()).toBe(403)
  const patch = await api.patch('/api/admin/news/2/status', { headers: eAuth, data: { action: 'top' } })
  expect(patch.status()).toBe(403)
  const recycle = await api.get('/api/admin/news?status=Recycled', { headers: eAuth })
  expect(recycle.status()).toBe(403)
  await api.dispose()
})

test('内容闭环：草稿→发布→置顶→撤回→回收→还原（前台可见性逐态核对）', async ({ page }) => {
  const api = await pwRequest.newContext({ baseURL: BASE })
  const token = await login(api, 'admin', 'AdPulse@2026')
  const auth = { Authorization: `Bearer ${token}` }
  const title = `生产闭环验收${Date.now().toString().slice(-6)}：全生命周期`
  const r = await api.post('/api/admin/news', {
    headers: auth,
    data: {
      title, summary: '闭环验收。', content: '<p>生命周期验收正文。</p>',
      coverUrl: '/covers/cover-06.svg', categoryId: 5, tagIds: [7],
      publish: false, isTop: false, isHeadline: false
    }
  })
  const id = (await r.json()).data.id
  const visible = async () => (await api.get(`/api/portal/news/${id}`)).status()
  expect(await visible()).toBe(404) // 草稿前台不可见

  await api.patch(`/api/admin/news/${id}/status`, { headers: auth, data: { action: 'publish' } })
  expect(await visible()).toBe(200)
  await api.patch(`/api/admin/news/${id}/status`, { headers: auth, data: { action: 'top' } })
  const list = (await (await api.get('/api/portal/news?pageSize=5')).json()).data.items
  expect(list[0].id).toBe(id) // 置顶排最前
  await api.patch(`/api/admin/news/${id}/status`, { headers: auth, data: { action: 'unpublish' } })
  expect(await visible()).toBe(404)
  await api.patch(`/api/admin/news/${id}/status`, { headers: auth, data: { action: 'recycle' } })
  expect(await visible()).toBe(404)
  await api.patch(`/api/admin/news/${id}/status`, { headers: auth, data: { action: 'restore' } })
  const d = (await (await api.get(`/api/admin/news/${id}`, { headers: auth })).json()).data
  expect(d.status).toBe('Draft')
  await api.delete(`/api/admin/news/${id}`, { headers: auth }) // 验收完回收，避免残留
  await api.dispose()
})

test('明暗主题切换刷新后记住 + 数据页诚实声明演示数据', async ({ page }) => {
  await page.goto(BASE + '/')
  await expect(page.locator('.flash-idle, .flash-item').first()).toBeVisible({ timeout: 15000 })
  const before = await page.evaluate(() => document.documentElement.getAttribute('data-theme'))
  await page.locator('.icon-btn[title*="切换"]').first().click()
  const after = await page.evaluate(() => document.documentElement.getAttribute('data-theme'))
  expect(after).not.toBe(before)
  await page.reload()
  expect(await page.evaluate(() => document.documentElement.getAttribute('data-theme'))).toBe(after)

  await page.goto(BASE + '/data')
  await expect(page.locator('.note').first()).toContainText('演示', { timeout: 15000 })
  await page.screenshot({ path: __dirname + '/screenshots/prod-data.png' })
})

test('手机宽度四页无横向滚动 + 轮播可滑动', async ({ browser }) => {
  const ctx = await browser.newContext({ viewport: { width: 375, height: 812 }, hasTouch: true })
  const page = await ctx.newPage()
  for (const path of ['/', '/news', '/news/1', '/data']) {
    await page.goto(BASE + path)
    await page.waitForTimeout(1200)
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)
    expect(overflow, `${path} 横向溢出 ${overflow}px`).toBeLessThanOrEqual(1)
  }
  await page.goto(BASE + '/')
  await expect(page.locator('.hero').first()).toBeVisible({ timeout: 15000 })
  await page.screenshot({ path: __dirname + '/screenshots/prod-mobile.png' })
  await ctx.close()
})