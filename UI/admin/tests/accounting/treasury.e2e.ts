import { expect, test, type Page } from "@playwright/test";

const accountId = "11111111-1111-4111-8111-111111111111";
const cashAccountId = "15151515-1515-4151-8151-151515151515";
const bankAccountId = "16161616-1616-4161-8161-161616161616";
const secondBankAccountId = "17171717-1717-4171-8171-171717171717";
const receivableId = "18181818-1818-4181-8181-181818181818";
const paymentId = "20202020-2020-4202-8202-202020202020";
const runtimeErrors = new WeakMap<Page, string[]>();

test.beforeEach(async ({ context, page }) => {
  await context.addCookies([{ name: "ecommerce_admin_access", value: "fixture-access-token", url: "http://localhost:4321", httpOnly: true, sameSite: "Lax" }]);
  await page.request.post("http://127.0.0.1:4320/__test/reset");
  const errors: string[] = []; runtimeErrors.set(page, errors);
  page.on("console", (message) => { if (message.type() === "error") errors.push(message.text()); }); page.on("pageerror", (error) => errors.push(error.message));
});
test.afterEach(async ({ page }) => expect(runtimeErrors.get(page) ?? [], "browser console/page errors").toEqual([]));

test("creates a cent-exact customer collection once and renders the receipt", async ({ page }, testInfo) => {
  await page.goto(`/accounting/payments/new?type=1&currentAccountId=${accountId}`);
  await expect(page.getByRole("heading", { name: "Yeni ödeme veya tahsilat" })).toBeVisible();
  await page.getByLabel("İşlem tutarı *").fill("100");
  await page.getByLabel("Kasa veya banka hesabı *").selectOption(`cash:${cashAccountId}`);
  await page.locator(`[name="allocation:${receivableId}"]`).fill("100");
  const submit = page.getByRole("button", { name: "Tahsilatı kaydet" }); await submit.evaluate((element: HTMLButtonElement) => { element.click(); element.click(); });
  await expect(page).toHaveURL(new RegExp(`${paymentId}\\?created=1$`), { timeout: 30_000 });
  await expect(page.getByRole("heading", { name: "Müşteri tahsilatı" })).toBeVisible();
  await expect(page.getByText("₺100,00", { exact: true }).first()).toBeVisible();
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json(); expect(state.paymentCreateRequests).toBe(1);
  await page.screenshot({ path: testInfo.outputPath("payment-receipt.png"), fullPage: true, caret: "initial" });
});

test("cancels through the payment lifecycle and does not call cancelled value an advance", async ({ page }) => {
  await page.goto(`/accounting/payments/new?type=1&currentAccountId=${accountId}`); await page.getByLabel("İşlem tutarı *").fill("100"); await page.getByLabel("Kasa veya banka hesabı *").selectOption(`cash:${cashAccountId}`); await page.locator(`[name="allocation:${receivableId}"]`).fill("100"); await page.getByRole("button", { name: "Tahsilatı kaydet" }).click(); await expect(page).toHaveURL(new RegExp(paymentId));
  await page.getByRole("button", { name: "Ödemeyi iptal et" }).click(); await page.getByLabel("İptal gerekçesi *").fill("Kontrollü E2E iptali"); await page.getByRole("button", { name: "İptali onayla" }).click();
  await expect(page.getByText("İptal edildi", { exact: true })).toBeVisible(); await expect(page.getByText("Uygulanamaz", { exact: true })).toBeVisible(); await expect(page.getByText("kullanılabilir avans anlamına gelmez", { exact: false })).toBeVisible();
});

test("renders the treasury control desk and creates a safe manual cash movement", async ({ page }, testInfo) => {
  await page.goto("/accounting/treasury"); await expect(page.getByRole("heading", { name: "Kasa ve Banka" })).toBeVisible(); await expect(page.getByText("API bakiyesi").first()).toBeVisible();
  await page.getByRole("link", { name: "Manuel hareket" }).first().click(); await page.getByLabel("İşlem türü *").selectOption("10"); await page.getByLabel("Finans hesabı *").selectOption(`cash:${cashAccountId}`); await page.getByLabel("Tutar *").fill("50.25"); await page.getByRole("button", { name: "Hareketi kaydet" }).click();
  await expect(page).toHaveURL(new RegExp(`/accounting/treasury/cash/${cashAccountId}`)); await expect(page.getByText("Finans hareketi kaydedildi", { exact: false })).toBeVisible(); await expect(page.getByText("+₺50,25", { exact: true })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath("cash-statement.png"), fullPage: true, caret: "initial" });
});

test("creates one atomic transfer and keeps transfer legs read-only", async ({ page }, testInfo) => {
  await page.goto("/accounting/treasury?view=transfer"); await page.getByLabel("Kaynak banka *").selectOption(bankAccountId); await page.getByLabel("Hedef banka *").selectOption(secondBankAccountId); await page.getByLabel("Tutar *").fill("125"); await page.getByRole("button", { name: "Transferi kaydet" }).click();
  await expect(page).toHaveURL(new RegExp(`/accounting/treasury/bank/${bankAccountId}`)); await expect(page.getByText("Banka transfer çıkışı")).toBeVisible(); await expect(page.getByText("Transfer bacağı ayrı terslenmez")).toBeVisible(); await expect(page.getByRole("button", { name: "Ters kayıt" })).toHaveCount(1);
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json(); expect(state.transferCreateRequests).toBe(1);
  await page.screenshot({ path: testInfo.outputPath("bank-transfer-statement.png"), fullPage: true, caret: "initial" });
});
