import { expect, test, type Page } from "@playwright/test";

const accountId = "11111111-1111-4111-8111-111111111111";
const variantId = "77777777-7777-4777-8777-777777777777";
const orderId = "12121212-1212-4121-8121-121212121212";
const invoiceId = "13131313-1313-4131-8131-131313131313";
const runtimeErrors = new WeakMap<Page, string[]>();

test.beforeEach(async ({ context, page }) => {
  await context.addCookies([{ name: "ecommerce_admin_access", value: "fixture-access-token", url: "http://localhost:4321", httpOnly: true, sameSite: "Lax" }]);
  await page.request.post("http://127.0.0.1:4320/__test/reset");
  const errors: string[] = []; runtimeErrors.set(page, errors);
  page.on("console", (message) => { if (message.type() === "error") errors.push(message.text()); });
  page.on("pageerror", (error) => errors.push(error.message));
});
test.afterEach(async ({ page }) => expect(runtimeErrors.get(page) ?? [], "browser console/page errors").toEqual([]));

test("keeps accounting sales distinct from ecommerce orders and exposes both registers", async ({ page }, testInfo) => {
  await page.goto("/accounting/sales-orders");
  await expect(page.getByRole("heading", { name: "Muhasebe Satışları" })).toBeVisible();
  await expect(page.getByRole("link", { name: "SAT-FIX-001" })).toBeVisible();
  await expect(page.getByText("E-ticaret siparişlerinden ayrı", { exact: false })).toBeVisible();
  await expect(page.locator('nav[aria-label="Ana navigasyon"] [aria-current="page"]')).toHaveCount(2);
  await page.screenshot({ path: testInfo.outputPath("sales-order-register.png"), fullPage: true, caret: "initial" });
  await page.goto("/accounting/sales-invoices");
  await expect(page.getByRole("heading", { name: "Henüz satış faturası bulunmuyor" })).toBeVisible();
});

test("creates one idempotent accounting sale intent and lands on authoritative detail", async ({ page }) => {
  await page.goto("/accounting/sales-orders/new");
  await page.getByLabel("Müşteri cari hesabı *").selectOption(accountId);
  await page.getByLabel("Satış numarası").fill("SAT-E2E-001");
  await page.getByLabel("Ürün varyantı *").selectOption(variantId);
  await page.getByLabel("Birim fiyat *").fill("250");
  const create = page.getByRole("button", { name: "Muhasebe satışı oluştur" });
  await create.evaluate((element: HTMLButtonElement) => { element.click(); element.click(); });
  await expect(page).toHaveURL(new RegExp(`${orderId}\\?created=1$`), { timeout: 30_000 });
  await expect(page.getByRole("status")).toContainText("taslak olarak oluşturuldu");
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json();
  expect(state.salesCreateRequests).toBe(1);
});

test("creates one direct invoice intent with its linked accounting sale", async ({ page }) => {
  await page.goto("/accounting/sales-invoices/new");
  await page.getByLabel("Müşteri cari hesabı *").selectOption(accountId);
  await page.getByLabel("Satış numarası").fill("SAT-DIRECT-001");
  await page.getByLabel("Ürün varyantı *").selectOption(variantId);
  await page.getByLabel("Birim fiyat *").fill("300");
  await page.getByLabel("Fatura numarası").fill("F-DIRECT-001");
  const create = page.getByRole("button", { name: "Satış faturası oluştur" });
  await create.evaluate((element: HTMLButtonElement) => { element.click(); element.click(); });
  await expect(page).toHaveURL(new RegExp(`${invoiceId}\\?created=1$`), { timeout: 30_000 });
  await expect(page.getByRole("status")).toContainText("bağlı muhasebe satışı taslak olarak oluşturuldu");
  await expect(page.getByRole("link", { name: "Muhasebe satışını aç" })).toBeVisible();
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json();
  expect(state.salesInvoiceCreateRequests).toBe(1);
});

test("posts once, creates a posted invoice from the order, and cancels only through the order", async ({ page }, testInfo) => {
  await page.goto(`/accounting/sales-orders/${orderId}`);
  await page.getByRole("button", { name: "Muhasebeleştir", exact: true }).click();
  await page.getByRole("button", { name: "Onayla ve muhasebeleştir" }).click();
  await expect(page.getByText("Post edildi", { exact: true }).first()).toBeVisible();
  await expect(page.getByRole("heading", { name: "Stok ve FIFO denetim izi" })).toBeVisible();
  await expect(page.getByText("₺120,00", { exact: false }).first()).toBeVisible();
  await page.getByLabel("Fatura numarası *").fill("F-E2E-001");
  await page.getByRole("button", { name: "Satış faturası oluştur" }).click();
  await expect(page).toHaveURL(new RegExp(`${invoiceId}\\?createdFromOrder=1$`), { timeout: 30_000 });
  await expect(page.getByRole("status")).toContainText("doğrudan post edilmiş");
  await expect(page.getByRole("button", { name: /İptal/ })).toHaveCount(0);
  await expect(page.getByText("Fatura ayrı iptal edilemez.")).toBeVisible();
  await page.getByRole("link", { name: "İptal için bağlı satışa git" }).click();
  await page.getByRole("button", { name: "Muhasebe satışını iptal et" }).click();
  await page.getByLabel("İptal gerekçesi *").fill("E2E kontrollü iptal");
  await page.getByRole("button", { name: "İptali onayla" }).click();
  await expect(page.getByText("İptal edildi", { exact: true }).first()).toBeVisible();
  await expect(page.getByText("E2E kontrollü iptal")).toBeVisible();
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json();
  expect(state.salesPostRequests).toBe(1); expect(state.salesInvoiceCreateRequests).toBe(1); expect(state.salesCancelRequests).toBe(1);
  await page.screenshot({ path: testInfo.outputPath("sales-order-cancelled.png"), fullPage: true, caret: "initial" });
});
