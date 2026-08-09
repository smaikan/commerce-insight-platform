import { defineConfig } from "@playwright/test";

const baseURL = process.env.ADMIN_BASE_URL;

if (!baseURL) {
  throw new Error("ADMIN_BASE_URL must point to a running production admin application.");
}

if (/localhost:3001/i.test(baseURL) && process.env.ALLOW_DEV_PERFORMANCE_TEST !== "true") {
  throw new Error("Refusing to measure next dev. Start `pnpm build; pnpm start` and set ADMIN_BASE_URL.");
}

export default defineConfig({
  testDir: "./tests/performance",
  testMatch: "**/*.e2e.ts",
  timeout: 45_000,
  retries: 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL,
    trace: "on",
    screenshot: "only-on-failure",
    video: "off",
  },
  projects: [{ name: "chromium", use: { browserName: "chromium", viewport: { width: 1440, height: 900 } } }],
});
