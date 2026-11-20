import { test, expect, request as pwRequest, type Page } from '@playwright/test'

// 终验剧本补漏项（走 nginx 生产环境）：后台重新生成按钮 UI 实测 + 断网重连
const BASE = 'http://127.0.0.1'

async function login(page: Page, name: string, pwd: string) {
  await page.goto(BASE + '/admin/login')
  await page.locator('input[placeholder="用户名"]').fill(name)
  await page.locator('input[placeholder="密码"]').fill(pwd)
  await page.locator('button', { hasText: '登 录' }).click()
  await expect(page).toHaveURL(/\/admin(?!\/login)/, { timeout: 15000 })
}

test('后台编辑页"重新生成"按钮：状态流转 生成中→规则生成（UI实测）', async ({ page }) => {
  await login(page, 'admin', 'AdPulse@2026')
  await page.goto(BASE + '/admin/news/2/edit')
  await expect(page.locator('button', { hasText: '重新生成 AI 摘要' })).toBeVisible({ timeout: 15000 })
  await page.locator('button', { hasText: '重新生成 AI 摘要' }).click()
  await expect(page.locator('.el-tag', { hasText: '生成中' })).toBeVisible({ timeout: 8000 })
  // 生产无密钥 → 规则小编秒级兜底，轮询 3s 刷新状态
  await expect(page.locator('.el-tag', { hasText: '规则生成' })).toBeVisible({ timeout: 15000 })
  await page.screenshot({ path: __dirname + '/screenshots/admin-ai-regen.png' })
})

test('nginx 生产链路断网重连：恢复后仍能收到新发布快讯', async ({ browser }) => {
  const api = await pwRequest.newContext({ baseURL: BASE })
  const r = await api.post('/api/auth/login', { data: { userName: 'admin', password: 'AdPulse@2026' } })
  const token = (await r.json()).data.token
  const auth = { Authorization: `Bearer ${token}` }

  const ctx = await browser.newContext()
  const page = await ctx.newPage()
  await page.goto(BASE + '/')
  await expect(page.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })

  await ctx.setOffline(true)
  await page.waitForTimeout(5000)
  await ctx.setOffline(false)
  await page.waitForTimeout(8000)

  const title = `终验重连${Date.now().toString().slice(-6)}：生产断网恢复`
  const pub = await api.post('/api/admin/news', {
    headers: auth,
    data: {
      title, summary: '终验。', content: '<p>终验断网重连正文。</p>',
      coverUrl: '/covers/cover-08.svg', categoryId: 2, tagIds: [3],
      publish: true, isTop: false, isHeadline: false
    }
  })
  const id = (await pub.json()).data.id
  await expect(page.locator('.flash-item')).toContainText(title, { timeout: 20000 })
  await api.delete(`/api/admin/news/${id}`, { headers: auth })
  await ctx.close(); await api.dispose()
})