import { expect, test } from "@playwright/test";

const email = process.env.ADMIN_TEST_EMAIL;
const password = process.env.ADMIN_TEST_PASSWORD;

test.describe("Sipariş ekranı performansı", () => {
  test.skip(!email || !password, "ADMIN_TEST_EMAIL ve ADMIN_TEST_PASSWORD güvenli test ortamında tanımlanmalıdır.");

  test("production /orders navigasyon zamanlamasını kaydeder", async ({ page }, testInfo) => {
    await page.goto("/login", { waitUntil: "domcontentloaded" });
    await page.getByLabel("E-posta adresi").fill(email!);
    await page.getByLabel("Parola").fill(password!);

    await Promise.all([
      page.waitForURL(/\/(dashboard|orders)(?:\?|$)/),
      page.getByRole("button", { name: "Giriş yap" }).click(),
    ]);

    await page.goto("/orders", { waitUntil: "networkidle" });
    await expect(page.getByRole("heading", { name: "Siparişler" })).toBeVisible();

    const timing = await page.evaluate(() => {
      const navigation = performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming | undefined;
      if (!navigation) throw new Error("Navigation Timing entry was not available.");

      return {
        url: location.pathname,
        domContentLoadedMs: Number(navigation.domContentLoadedEventEnd.toFixed(1)),
        responseStartMs: Number(navigation.responseStart.toFixed(1)),
        loadEventEndMs: Number(navigation.loadEventEnd.toFixed(1)),
        transferSizeBytes: navigation.transferSize,
      };
    });

    await testInfo.attach("orders-navigation-timing.json", {
      body: JSON.stringify(timing, null, 2),
      contentType: "application/json",
    });
  });
});
