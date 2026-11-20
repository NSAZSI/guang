import { test, expect } from '@playwright/test'

// 验收清单：前台页面挂机 10 分钟，控制台无报错、快讯条仍在（心跳保活）
// 注意：必须跑在生产构建上（IDLE_BASE 指向 preview/nginx），dev server 的 HMR 会重置页面计数器
const BASE = process.env.IDLE_BASE || 'http://localhost:4173'

test('挂机 10 分钟：心跳保活 + 控制台零报错', async ({ page }) => {
  test.setTimeout(680000)
  const errors: string[] = []
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()) })
  page.on('pageerror', e => errors.push(String(e)))

  await page.goto(BASE + '/')
  await expect(page.locator('.flash-idle')).toContainText('通道已连接', { timeout: 15000 })

  await page.waitForTimeout(600000) // 真实挂机 10 分钟

  // 快讯条仍在且连接未断（hello + 约 40 次 ping）
  const dbg = await page.evaluate(() => (window as any).__adpulseFlash)
  expect(dbg.count).toBeGreaterThanOrEqual(35)
  expect(Date.now() - dbg.lastEvent).toBeLessThan(20000)
  await expect(page.locator('.flash .dot.off')).toHaveCount(0)
  expect(errors, `控制台报错：${errors.join(' | ')}`).toHaveLength(0)
  console.log(`10 分钟挂机通过：收到 ${dbg.count} 个事件，控制台 0 报错`)
})