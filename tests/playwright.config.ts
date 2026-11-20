import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: '.',
  timeout: 90000,
  retries: 0,
  reporter: 'list',
  use: {
    headless: true,
    viewport: { width: 1440, height: 900 },
    launchOptions: { args: ['--no-sandbox', '--disable-dev-shm-usage'] }
  }
})