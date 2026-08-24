import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/accounting",
  testMatch: "**/*.e2e.ts",
  timeout: 60_000,
  expect: { timeout: 30_000 },
  workers: 1,
  retries: 0,
  reporter: [["list"]],
  outputDir: "test-results/accounting",
  webServer: [
    { command: "node tests/accounting/mock-api.mjs", port: 4320, reuseExistingServer: false, timeout: 30_000 },
    { command: "INTERNAL_API_BASE_URL=http://127.0.0.1:4320 pnpm exec next dev --port 4321", port: 4321, reuseExistingServer: false, timeout: 120_000 },
  ],
  use: { baseURL: "http://localhost:4321", trace: "retain-on-failure", screenshot: "only-on-failure", video: "off" },
  projects: [
    { name: "desktop-chromium", use: { browserName: "chromium", viewport: { width: 1440, height: 1000 } } },
    { name: "mobile-chromium", use: { browserName: "chromium", viewport: { width: 390, height: 844 }, hasTouch: true } },
  ],
});
