import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/contact-messages",
  testMatch: "**/*.e2e.ts",
  timeout: 60_000,
  workers: 1,
  retries: 0,
  reporter: [["list"]],
  outputDir: "test-results/contact-messages",
  webServer: [
    { command: "node tests/contact-messages/mock-api.mjs", port: 4310, reuseExistingServer: false, timeout: 30_000 },
    { command: "INTERNAL_API_BASE_URL=http://127.0.0.1:4310 pnpm exec next dev --port 4311", port: 4311, reuseExistingServer: false, timeout: 120_000 },
  ],
  use: {
    baseURL: "http://localhost:4311",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "off",
  },
  projects: [
    { name: "desktop-chromium", use: { browserName: "chromium", viewport: { width: 1440, height: 1000 } } },
    { name: "mobile-chromium", use: { browserName: "chromium", viewport: { width: 390, height: 844 }, isMobile: true } },
  ],
});
