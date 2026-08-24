import { expect, test, type Page } from "@playwright/test";

const productVariantId = "77777777-7777-4777-8777-777777777777";
const runtimeErrors = new WeakMap<Page, string[]>();

test.beforeEach(async ({ context, page }) => {
  await context.addCookies([{ name: "ecommerce_admin_access", value: "fixture-access-token", url: "http://localhost:4321", httpOnly: true, sameSite: "Lax" }]);
  await page.request.post("http://127.0.0.1:4320/__test/reset");
  const errors: string[] = []; runtimeErrors.set(page, errors);
  page.on("console", (message) => { if (message.type() === "error") errors.push(message.text()); }); page.on("pageerror", (error) => errors.push(error.message));
});
test.afterEach(async ({ page }) => expect(runtimeErrors.get(page) ?? [], "browser console/page errors").toEqual([]));

// Burada rapor dizininin bütün sözleşme başlıklarını ürün kataloğu görünümüne dönüşmeden sunduğunu doğruluyorum.
test("opens the complete accounting report catalog", async ({ page }, testInfo) => {
  await page.goto("/accounting/reports");
  await expect(page.getByRole("heading", { name: "Muhasebe Raporları" })).toBeVisible();
  await expect(page.getByRole("main").getByRole("link", { name: /Satışlar/ })).toBeVisible();
  await expect(page.getByRole("main").getByRole("link", { name: /Satış KDV/ })).toBeVisible();
  await expect(page.getByText("Kimlik ile çalışır").first()).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath("report-catalog.png"), fullPage: true, caret: "initial" });
});

// Burada satış raporunun kendi filtre ve kolonlarını kullandığını, erişilebilir geniş tablonun mobilde taşınabildiğini doğruluyorum.
test("renders report-specific sales columns without fake totals", async ({ page }, testInfo) => {
  await page.goto("/accounting/reports/sales?search=RPR");
  await expect(page.getByRole("heading", { name: "Satış raporu" })).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Satış maliyeti" })).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Brüt kâr" })).toBeVisible();
  await expect(page.getByText("1 kayıt")).toBeVisible();
  await expect(page.getByText(/genel toplam/i)).toHaveCount(0);
  const region = page.getByRole("region", { name: /Satış raporu; yatay kaydırılabilir/ });
  await region.focus();
  expect(await region.evaluate((element) => element.scrollWidth >= element.clientWidth)).toBe(true);
  await page.screenshot({ path: testInfo.outputPath("sales-report.png"), fullPage: true, caret: "initial" });
});

// Burada geçerli token ile açılış maliyetinin bir kez güncellendiğini ve geçmişin görünür kaldığını doğruluyorum.
test("updates the opening FIFO cost once", async ({ page }, testInfo) => {
  await page.goto(`/accounting/costing?productVariantId=${productVariantId}`);
  await expect(page.getByRole("heading", { name: "FIFO Maliyet Yönetimi" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Varyant maliyet geçmişi" })).toBeVisible();
  await page.getByLabel("KDV hariç birim maliyet *").fill("82.25");
  await page.getByLabel("KDV dahil birim maliyet").fill("98.70");
  await page.getByRole("button", { name: "Maliyeti güncelle" }).click();
  await expect(page.getByRole("status")).toContainText("güncellendi");
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json(); expect(state.openingCostPatchRequests).toBe(1);
  await page.screenshot({ path: testInfo.outputPath("opening-cost-updated.png"), fullPage: true, caret: "initial" });
});

// Burada stale conflict sonrasında taslağın korunduğunu ve ikinci PATCH'in açık onaydan önce gönderilmediğini doğruluyorum.
test("requires explicit confirmation after a stale cost conflict", async ({ page }) => {
  await page.request.post("http://127.0.0.1:4320/__test/stale-cost");
  await page.goto(`/accounting/costing?productVariantId=${productVariantId}`);
  await page.getByLabel("KDV hariç birim maliyet *").fill("84.50");
  await page.getByRole("button", { name: "Maliyeti güncelle" }).click();
  await expect(page.getByText("Başka bir değişiklik algılandı", { exact: true })).toBeVisible();
  await expect(page.getByText("84.50", { exact: true })).toBeVisible();
  let state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json(); expect(state.openingCostPatchRequests).toBe(1);
  await page.getByRole("button", { name: "Güncel kaydı kullanıp maliyeti uygula" }).click();
  await expect(page.getByRole("status")).toContainText("güncellendi");
  state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json(); expect(state.openingCostPatchRequests).toBe(2);
});
