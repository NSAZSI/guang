import { test, expect, type Page } from '@playwright/test'

const BASE = 'http://localhost:5173'
const SHOTS = __dirname + '/screenshots'

function watchErrors(page: Page): string[] {
  const errors: string[] = []
  page.on('pageerror', (e) => errors.push(String(e)))
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()) })
  return errors
}

test('首页：轮播/分类Tab/热榜/统计数字，切换Tab内容变化', async ({ page }) => {
  const errors = watchErrors(page)
  await page.goto(BASE + '/')
  await expect(page.locator('.hero .slide img').first()).toBeVisible({ timeout: 20000 })
  await expect(page.locator('.tabs button')).toHaveCount(5)
  await expect(page.locator('.hot li')).toHaveCount(10)
  await expect(page.locator('.counter')).toHaveCount(3)
  const firstTitle = await page.locator('.cards .news-card h3').first().textContent()
  await page.locator('.tabs button').nth(2).click()
  await page.waitForTimeout(400)
  const switched = await page.locator('.cards .news-card h3').first().textContent()
  expect(switched).not.toBe(firstTitle)
  await page.screenshot({ path: SHOTS + '/home-desktop.png', fullPage: false })
  expect(errors).toEqual([])
})

test('列表页：搜索高亮/分页/排序', async ({ page }) => {
  const errors = watchErrors(page)
  await page.goto(BASE + '/news')
  await expect(page.locator('.news-card').first()).toBeVisible({ timeout: 20000 })
  await page.locator('.search input').fill('品牌')
  await page.locator('.search input').press('Enter')
  await expect(page.locator('.result-info')).toContainText('品牌')
  await expect(page.locator('mark.hl').first()).toBeVisible()
  await expect(page.locator('.el-pagination')).toBeVisible()
  await page.locator('.sort button', { hasText: '最热' }).click()
  await page.waitForTimeout(500)
  await expect(page.locator('.news-card').first()).toBeVisible()
  expect(errors).toEqual([])
})

test('详情页：富文本/相关推荐/上一篇下一篇，直接刷新不404', async ({ page }) => {
  const errors = watchErrors(page)
  await page.goto(BASE + '/news')
  await expect(page.locator('.news-card').first()).toBeVisible({ timeout: 20000 })
  await page.locator('.news-card').first().click()
  await expect(page).toHaveURL(/\/news\/\d+/)
  await expect(page.locator('.title')).toBeVisible()
  expect(await page.locator('.rich-content p').count()).toBeGreaterThanOrEqual(3)
  await expect(page.locator('.rel-title')).toBeVisible()
  await expect(page.locator('.ai-card')).toBeVisible()
  const url = page.url()
  await page.goto(url) // 直接访问（history 路由回退）
  await expect(page.locator('.title')).toBeVisible({ timeout: 15000 })
  await page.reload()
  await expect(page.locator('.title')).toBeVisible({ timeout: 15000 })
  await page.screenshot({ path: SHOTS + '/detail-desktop.png' })
  expect(errors).toEqual([])
})

test('数据页：4张图表渲染 + 演示数据声明', async ({ page }) => {
  const errors = watchErrors(page)
  await page.goto(BASE + '/data')
  await expect(page.locator('.chart-card canvas')).toHaveCount(4, { timeout: 20000 })
  await expect(page.locator('.note')).toContainText('演示')
  await page.screenshot({ path: SHOTS + '/data-desktop.png' })
  expect(errors).toEqual([])
})

test('明暗主题切换并持久化', async ({ page }) => {
  await page.goto(BASE + '/')
  await expect(page.locator('.hero')).toBeVisible({ timeout: 20000 })
  await page.locator('.icon-btn').first().click()
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'light')
  await page.reload()
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'light')
  await page.locator('.icon-btn').first().click()
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark')
})

test('手机端375px：四页无横向滚动，抽屉导航可用', async ({ browser }) => {
  const ctx = await browser.newContext({ viewport: { width: 375, height: 812 }, isMobile: true, hasTouch: true })
  const page = await ctx.newPage()
  const errors = watchErrors(page)
  for (const path of ['/', '/news', '/data']) {
    await page.goto(BASE + path)
    await page.waitForTimeout(1800)
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1)
    expect(overflow, `页面 ${path} 出现横向滚动`).toBe(false)
  }
  await page.goto(BASE + '/')
  await expect(page.locator('.hero')).toBeVisible({ timeout: 20000 })
  await page.locator('.mobile-menu').click()
  await expect(page.locator('.drawer-link').first()).toBeVisible()
  await page.locator('.drawer-link', { hasText: '行业数据' }).click()
  await expect(page).toHaveURL(/\/data/)
  await page.goto(BASE + '/')
  await page.waitForTimeout(1500)
  await page.screenshot({ path: SHOTS + '/home-mobile.png' })
  expect(errors).toEqual([])
  await ctx.close()
})