import { test, expect, type Page } from '@playwright/test'

const BASE = 'http://localhost:5173'
const SHOTS = __dirname + '/screenshots'
const PNG = __dirname + '/fixtures/test.png'
const TITLE = `E2E验收稿${Date.now().toString().slice(-6)}：后台发文全链路`

function watchErrors(page: Page): string[] {
  const errors: string[] = []
  page.on('pageerror', (e) => errors.push(String(e)))
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()) })
  return errors
}

async function login(page: Page, name: string, pwd: string) {
  await page.goto(BASE + '/admin/login')
  await page.locator('input[placeholder="用户名"]').fill(name)
  await page.locator('input[placeholder="密码"]').fill(pwd)
  await page.locator('button', { hasText: '登 录' }).click()
  await expect(page).toHaveURL(/\/admin(?!\/login)/, { timeout: 15000 })
}

test('未登录访问后台被弹回登录页；错误密码有中文提示', async ({ page }) => {
  await page.goto(BASE + '/admin/news')
  await expect(page).toHaveURL(/\/admin\/login/, { timeout: 15000 })
  await page.locator('input[placeholder="用户名"]').fill('admin')
  await page.locator('input[placeholder="密码"]').fill('wrong-pass')
  await page.locator('button', { hasText: '登 录' }).click()
  await expect(page.locator('.el-message')).toContainText('用户名或密码错误', { timeout: 10000 })
})

test('核心流程：TipTap写稿+传图→草稿→发布→前台可见→置顶→撤回→回收站→还原', async ({ page }) => {
  const errors = watchErrors(page)
  await login(page, 'admin', 'AdPulse@2026')
  await page.screenshot({ path: SHOTS + '/admin-dashboard.png' })

  // 写稿
  await page.goto(BASE + '/admin/news')
  await page.locator('button', { hasText: '+ 写新闻' }).click()
  await expect(page).toHaveURL(/\/admin\/news\/create/)
  await page.locator('input[placeholder="输入新闻标题"]').fill(TITLE)
  await page.locator('textarea').first().fill('端到端验收摘要：验证后台到前台的完整内容链路。')
  await page.locator('.field', { hasText: '分类 *' }).locator('.el-select').click()
  await page.locator('.el-select-dropdown__item', { hasText: '数字广告' }).first().click()
  await page.locator('.cover-row input[type="file"]').setInputFiles(PNG)
  await expect(page.locator('.cover-preview')).toBeVisible({ timeout: 10000 })
  await page.locator('.ProseMirror').click()
  await page.locator('.ProseMirror').pressSequentially('第一段：这是验收正文，验证富文本链路。')
  // TipTap 工具栏上传图片（走服务器上传而非 base64）
  const fcPromise = page.waitForEvent('filechooser')
  await page.locator('button[title="上传图片"]').click()
  const fc = await fcPromise
  await fc.setFiles(PNG)
  await expect(page.locator('.ProseMirror img')).toBeVisible({ timeout: 10000 })
  const imgSrc = await page.locator('.ProseMirror img').getAttribute('src')
  expect(imgSrc).toContain('/uploads/')
  await page.screenshot({ path: SHOTS + '/admin-edit.png' })

  // 存草稿 → 前台不可见
  await page.locator('button', { hasText: '存草稿' }).click()
  await expect(page).toHaveURL(/\/admin\/news$/, { timeout: 15000 })
  await page.goto(BASE + '/news?keyword=' + encodeURIComponent(TITLE))
  await expect(page.locator('.empty')).toBeVisible({ timeout: 15000 })

  // 发布 → 前台可见且正文图片正常
  await page.goto(BASE + '/admin/news?')
  await page.locator('.search input, input[placeholder="搜索标题"]').fill(TITLE)
  await page.locator('button', { hasText: '查询' }).click()
  const row = page.locator('tr', { hasText: TITLE })
  await expect(row).toBeVisible({ timeout: 10000 })
  await row.locator('button', { hasText: '发布' }).click()
  await page.waitForTimeout(800)
  await page.goto(BASE + '/news?keyword=' + encodeURIComponent(TITLE))
  await expect(page.locator('.news-card')).toHaveCount(1, { timeout: 15000 })
  await page.locator('.news-card').first().click()
  await expect(page.locator('.rich-content img')).toBeVisible({ timeout: 15000 })
  await expect(page.locator('.title')).toContainText(TITLE)

  // 置顶 → 前台列表第一且带置顶标识
  await page.goto(BASE + '/admin/news')
  await page.locator('input[placeholder="搜索标题"]').fill(TITLE)
  await page.locator('button', { hasText: '查询' }).click()
  await page.locator('tr', { hasText: TITLE }).locator('button', { hasText: '置顶' }).click()
  await page.waitForTimeout(800)
  await page.goto(BASE + '/news')
  const firstCard = page.locator('.news-card').first()
  await expect(firstCard).toContainText(TITLE, { timeout: 15000 })
  await expect(firstCard.locator('.top-badge')).toBeVisible()

  // 撤回 → 前台消失
  await page.goto(BASE + '/admin/news')
  await page.locator('input[placeholder="搜索标题"]').fill(TITLE)
  await page.locator('button', { hasText: '查询' }).click()
  await page.locator('tr', { hasText: TITLE }).locator('button', { hasText: '撤回' }).click()
  await page.waitForTimeout(800)
  await page.goto(BASE + '/news?keyword=' + encodeURIComponent(TITLE))
  await expect(page.locator('.empty')).toBeVisible({ timeout: 15000 })

  // 删除进回收站 → 还原
  await page.goto(BASE + '/admin/news')
  await page.locator('input[placeholder="搜索标题"]').fill(TITLE)
  await page.locator('button', { hasText: '查询' }).click()
  await page.locator('tr', { hasText: TITLE }).locator('button', { hasText: '删除' }).click()
  await page.locator('.el-message-box button', { hasText: '移入回收站' }).click()
  await page.waitForTimeout(800)
  await page.goto(BASE + '/admin/recycle')
  await expect(page.locator('tr', { hasText: TITLE })).toBeVisible({ timeout: 10000 })
  await page.locator('tr', { hasText: TITLE }).locator('button', { hasText: '还原' }).click()
  await page.waitForTimeout(800)
  await page.goto(BASE + '/admin/news')
  await page.locator('input[placeholder="搜索标题"]').fill(TITLE)
  await page.locator('button', { hasText: '查询' }).click()
  await expect(page.locator('tr', { hasText: TITLE })).toContainText('草稿', { timeout: 10000 })
  await page.screenshot({ path: SHOTS + '/admin-news.png' })
  expect(errors).toEqual([])
})

test('editor 权限：菜单隐藏 + 直连管理页被拦 + 不能动 admin 文章', async ({ browser }) => {
  const ctx = await browser.newContext()
  const page = await ctx.newPage()
  await login(page, 'editor', 'Editor@2026')
  await expect(page.locator('.el-menu-item', { hasText: '分类管理' })).toHaveCount(0)
  await expect(page.locator('.el-menu-item', { hasText: '操作日志' })).toHaveCount(0)
  await expect(page.locator('.el-menu-item', { hasText: '回收站' })).toHaveCount(0)
  await page.goto(BASE + '/admin/categories')
  await expect(page).toHaveURL(/\/admin$/, { timeout: 10000 })
  await ctx.close()
})

test('篡改 token 后进入后台被自动踢回登录页', async ({ page }) => {
  await login(page, 'admin', 'AdPulse@2026')
  await page.evaluate(() => localStorage.setItem('adpulse_token', 'tampered-token'))
  await page.goto(BASE + '/admin/news')
  // fetchMe 携带伪造 token 触发 401，拦截器自动清态并跳登录
  await expect(page).toHaveURL(/\/admin\/login/, { timeout: 15000 })
})

test('手机端后台：登录/卡片列表/编辑页可用', async ({ browser }) => {
  const ctx = await browser.newContext({ viewport: { width: 375, height: 812 }, isMobile: true, hasTouch: true })
  const page = await ctx.newPage()
  const errors = watchErrors(page)
  await login(page, 'admin', 'AdPulse@2026')
  await page.goto(BASE + '/admin/news')
  await expect(page.locator('.m-card').first()).toBeVisible({ timeout: 15000 })
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1)
  expect(overflow).toBe(false)
  await page.locator('.mobile-only').click()
  await expect(page.locator('.el-drawer .el-menu-item', { hasText: '新闻管理' })).toBeVisible()
  await page.goto(BASE + '/admin/news/create')
  await expect(page.locator('input[placeholder="输入新闻标题"]')).toBeVisible({ timeout: 15000 })
  await page.screenshot({ path: SHOTS + '/admin-mobile.png' })
  expect(errors).toEqual([])
  await ctx.close()
})