import { test, expect } from '@playwright/test'

test('步骤1：骨架页显示后端健康结果且无控制台报错', async ({ page }) => {
  const errors: string[] = []
  page.on('pageerror', (e) => errors.push(String(e)))
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()) })
  await page.goto('http://localhost:5173/')
  await expect(page.locator('.ok')).toContainText('骨架已就绪', { timeout: 20000 })
  await expect(page.locator('.status')).toContainText('数据库连接：正常')
  expect(errors).toEqual([])
})