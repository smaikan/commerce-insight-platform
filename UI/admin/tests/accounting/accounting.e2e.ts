import { expect, test, type Page } from "@playwright/test";

const accountId = "11111111-1111-4111-8111-111111111111";
const runtimeErrors = new WeakMap<Page, string[]>();

test.beforeEach(async ({ context, page }) => {
  await context.addCookies([{ name: "ecommerce_admin_access", value: "fixture-access-token", url: "http://localhost:4321", httpOnly: true, sameSite: "Lax" }]);
  await page.request.post("http://127.0.0.1:4320/__test/reset");
  const errors: string[] = [];
  runtimeErrors.set(page, errors);
  page.on("console", (message) => { if (message.type() === "error") errors.push(message.text()); });
  page.on("pageerror", (error) => errors.push(error.message));
});

test.afterEach(async ({ page }) => expect(runtimeErrors.get(page) ?? [], "browser console/page errors").toEqual([]));

test("protects the accounting boundary without an admin cookie", async ({ browser }) => {
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto("/accounting");
  await expect(page).toHaveURL(/\/login\?.*reason=session_required/);
  await context.close();
});

test("renders the accounting workspace and selects only the specific current-account navigation", async ({ page }, testInfo) => {
  await page.goto("/accounting");
  await expect(page.getByRole("heading", { name: "Ön Muhasebe" })).toBeVisible();
  await expect(page.getByRole("region", { name: "Muhasebe operasyon kuyrukları" }).getByRole("heading")).toHaveCount(4);
  await page.getByRole("link", { name: "Cari hesaplar", exact: false }).last().click();
  await expect(page.getByRole("heading", { name: "Cari Hesaplar" })).toBeVisible();
  const activeLinks = page.locator('nav[aria-label="Ana navigasyon"] [aria-current="page"]');
  await expect(activeLinks).toHaveCount(2);
  expect(await activeLinks.evaluateAll((links) => links.every((link) => link.getAttribute("href") === "/accounting/current-accounts"))).toBe(true);
  await page.screenshot({ path: testInfo.outputPath("current-accounts.png"), fullPage: true, caret: "initial" });
});

test("canonicalizes out-of-range list and statement pages", async ({ page }) => {
  await page.goto("/accounting/current-accounts?pageNumber=9999");
  await expect(page).toHaveURL(/\/accounting\/current-accounts$/);
  await page.goto(`/accounting/current-accounts/${accountId}?statementPageNumber=9999`);
  await expect(page).toHaveURL(new RegExp(`${accountId}\\?statementPageNumber=2$`));
  await expect(page.getByText("Sayfa 2 / 2")).toBeVisible();
});

test("renders safe permission, rate-limit, server-error and not-found states", async ({ page }) => {
  await page.goto("/accounting/current-accounts?pageNumber=403");
  await expect(page.getByRole("heading", { name: "Bu alan için yetkiniz yok" })).toBeVisible();
  await page.goto("/accounting/current-accounts?pageNumber=429");
  await expect(page.getByText("API 60 saniye sonra yeniden denemenizi istiyor.")).toBeVisible();
  await page.goto("/accounting/current-accounts?pageNumber=500");
  await expect(page.getByText("İz: accounting-fixture-trace")).toBeVisible();
  await page.goto("/accounting/current-accounts/99999999-9999-4999-8999-999999999999");
  await expect(page.getByRole("heading", { name: "Cari hesap bulunamadı" })).toBeVisible();
});

test("renders streamed loading and authoritative empty states", async ({ page }) => {
  await page.request.post("http://127.0.0.1:4320/__test/delay");
  const navigation = page.goto("/accounting/current-accounts");
  await expect(page.getByLabel("Ön muhasebe yükleniyor")).toBeVisible();
  await navigation;
  await page.request.post("http://127.0.0.1:4320/__test/empty");
  await page.goto("/accounting/current-accounts");
  await expect(page.getByRole("heading", { name: "Henüz cari hesap bulunmuyor" })).toBeVisible();
});

test("preserves a rejected create draft and exposes linked field errors", async ({ page }) => {
  await page.goto("/accounting/current-accounts/new");
  await expect(page.getByRole("checkbox", { name: "Aktif cari hesap" })).toHaveCount(0);
  await page.getByLabel("Cari kodu").fill("VALID-001");
  await page.getByLabel("Cari unvanı").fill("Korunacak cari taslağı");
  await page.getByLabel("Bağlı kullanıcı ID").fill("invalid-user");
  await page.getByRole("button", { name: "Cari hesap oluştur" }).click();
  const fieldErrorLink = page.getByRole("alert").filter({ hasText: "kanonik" }).getByRole("link");
  await expect(fieldErrorLink).toHaveAttribute("href", "#userId");
  await fieldErrorLink.click();
  await expect(page.getByLabel("Bağlı kullanıcı ID")).toBeFocused();
  await page.getByLabel("Bağlı kullanıcı ID").fill("");
  await page.getByLabel("Cari kodu").fill("DUPLICATE");
  await page.getByRole("button", { name: "Cari hesap oluştur" }).click();
  await expect(page.getByRole("alert").filter({ hasText: "çakışıyor" })).toBeVisible();
  await expect(page.getByLabel("Cari kodu")).toHaveValue("DUPLICATE");
  await expect(page.getByLabel("Cari unvanı")).toHaveValue("Korunacak cari taslağı");
});

test("preserves Retry-After and the rate-limited draft", async ({ page }) => {
  await page.goto("/accounting/current-accounts/new");
  await page.getByLabel("Cari kodu").fill("RATE");
  await page.getByLabel("Cari unvanı").fill("Limitte korunacak taslak");
  await page.getByRole("button", { name: "Cari hesap oluştur" }).click();
  const alert = page.getByRole("alert").filter({ hasText: "İstek sınırına ulaşıldı" });
  await expect(alert).toContainText("45 saniye");
  await expect(page.getByLabel("Cari unvanı")).toHaveValue("Limitte korunacak taslak");
});

test("accepts the runtime 201 create response exactly once", async ({ page }) => {
  await page.goto("/accounting/current-accounts/new");
  await page.getByLabel("Cari kodu").fill("new-001");
  await page.getByLabel("Cari unvanı").fill("Yeni Fixture Cari");
  const button = page.getByRole("button", { name: "Cari hesap oluştur" });
  await button.evaluate((element: HTMLButtonElement) => { element.click(); element.click(); });
  await expect(page).toHaveURL(new RegExp(`${accountId}\\?created=1$`));
  await expect(page.getByRole("status")).toContainText("Cari hesap oluşturuldu");
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json();
  expect(state.createRequests).toBe(1);
});

test("renders the contract-safe statement and keeps the wide ledger usable", async ({ page }, testInfo) => {
  await page.goto(`/accounting/current-accounts/${accountId}`);
  await expect(page.getByRole("heading", { name: "Cari ekstre" })).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Borç" })).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Alacak" })).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Açık tutar" })).toBeVisible();
  await expect(page.getByRole("columnheader", { name: /Kaynak|Bakiye/ })).toHaveCount(0);
  const ledger = page.getByRole("region", { name: /Cari ekstre tablosu/ });
  await page.screenshot({ path: testInfo.outputPath("statement.png"), fullPage: true, caret: "initial" });
  await ledger.focus();
  await expect(ledger).toBeFocused();
});

test("creates a purchase invoice draft once and lands on authoritative detail", async ({ page }) => {
  await page.goto("/accounting/purchase-invoices/new");
  await page.getByLabel("Tedarikçi *").selectOption(accountId);
  await page.getByLabel("Fatura numarası").fill("ALIŞ-E2E-001");
  await page.getByLabel("Ürün varyantı *").selectOption("77777777-7777-4777-8777-777777777777");
  await expect(page.getByLabel("Tedarikçi *")).toHaveValue(accountId);
  await expect(page.getByLabel("Ürün varyantı *")).toHaveValue("77777777-7777-4777-8777-777777777777");
  const create = page.getByRole("button", { name: "Taslak oluştur" });
  await create.evaluate((element: HTMLButtonElement) => { element.click(); element.click(); });
  await expect(page).toHaveURL(/55555555-5555-4555-8555-555555555555\?created=1$/, { timeout: 30_000 });
  await expect(page.getByRole("status")).toContainText("tahsislerini tamamlayabilirsiniz");
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json();
  expect(state.purchaseCreateRequests).toBe(1);
});

test("blocks incomplete posting then refetches Posted and FIFO history after full allocation", async ({ page }, testInfo) => {
  await page.goto("/accounting/purchase-invoices/55555555-5555-4555-8555-555555555555");
  const post = page.getByRole("button", { name: "Muhasebeleştir", exact: true });
  await expect(post).toBeDisabled();
  await expect(page.getByText(/Tam tahsisi eksik satırlar: 1/)).toBeVisible();
  await page.getByText("Tahsis yönetimi").click();
  await page.getByLabel(/hareketinden Satır 1 tahsis miktarı/).fill("10");
  await page.getByRole("button", { name: "Tahsisleri kaydet" }).click();
  await expect(post).toBeEnabled();
  await post.click();
  await page.getByRole("button", { name: "Onayla ve muhasebeleştir" }).click();
  await expect(page.getByText("Post edildi", { exact: true }).first()).toBeVisible();
  await expect(page.getByRole("heading", { name: "FIFO maliyet etkisi" })).toBeVisible();
  await expect(page.getByText("₺1.200,00").last()).toBeVisible();
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json();
  expect(state.purchasePostRequests).toBe(1);
  await page.screenshot({ path: testInfo.outputPath("purchase-invoice-posted.png"), fullPage: true, caret: "initial" });
});

test("adds an invoice expense, refetches final cost and blocks unsafe posting", async ({ page }, testInfo) => {
  await page.goto("/accounting/purchase-invoices/55555555-5555-4555-8555-555555555555");
  await page.getByLabel("Kategori *").selectOption("99999999-9999-4999-8999-999999999999");
  await page.getByLabel("KDV hariç tutar *").fill("100");
  await page.getByRole("button", { name: "Gideri dağıt" }).click();
  await expect(page.getByText("Giderli fatura post edilemez.")).toBeVisible();
  await expect(page.getByText("₺1.100,00")).toBeVisible();
  await expect(page.getByRole("button", { name: "Muhasebeleştir" })).toBeDisabled();
  const state = await (await page.request.get("http://127.0.0.1:4320/__test/state")).json();
  expect(state.purchaseExpenseRequests).toBe(1);
  await page.screenshot({ path: testInfo.outputPath("purchase-invoice-expense-block.png"), fullPage: true, caret: "initial" });
});

test("creates an append-only general expense without exposing unsupported row actions", async ({ page }, testInfo) => {
  await page.goto("/accounting/expenses");
  await page.getByLabel("Kategori *").selectOption("99999999-9999-4999-8999-999999999999");
  await page.getByLabel("KDV hariç tutar *").fill("250");
  await page.getByLabel("Gider tarihi *").fill("2026-08-24");
  await page.getByLabel("Açıklama *").fill("E2E ofis gideri");
  await page.getByRole("button", { name: "Genel gideri kaydet" }).click();
  await expect(page.getByText("E2E ofis gideri")).toBeVisible();
  await expect(page.getByRole("button", { name: /Düzenle|Sil|İptal|Post/ })).toHaveCount(0);
  await page.getByRole("link", { name: "Kategoriler" }).click();
  await expect(page.getByRole("heading", { name: "Gider kategorisi oluştur" })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath("expenses-categories.png"), fullPage: true, caret: "initial" });
});
