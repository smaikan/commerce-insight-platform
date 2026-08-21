import { expect, test, type Page } from "@playwright/test";

const messageId = "11111111-1111-4111-8111-111111111111";
const runtimeErrors = new WeakMap<Page, string[]>();

test.beforeEach(async ({ context, page }) => {
  await context.addCookies([{ name: "ecommerce_admin_access", value: "fixture-access-token", url: "http://localhost:4311", httpOnly: true, sameSite: "Lax" }]);
  await page.request.post("http://127.0.0.1:4310/__test/reset");
  collectRuntimeErrors(page);
});

test.afterEach(async ({ page }) => {
  expect(runtimeErrors.get(page) ?? [], "browser console/page errors").toEqual([]);
});

test("protects direct route without an admin cookie", async ({ browser }) => {
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto("/contact-messages");
  await expect(page).toHaveURL(/\/login\?.*reason=session_required/);
  await context.close();
});

test("filters list, preserves return state and exposes operational detail", async ({ page }, testInfo) => {
  await page.goto("/contact-messages");
  await expect(page.getByRole("heading", { name: "İletişim Mesajları" })).toBeVisible();
  await expect(page.getByRole("link", { name: "CM-20260821-0001", exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Sonraki" }).click();
  await expect(page).toHaveURL(/pageNumber=2/);
  await page.goto("/contact-messages");
  await page.screenshot({ path: testInfo.outputPath("list-desktop.png"), fullPage: true, caret: "initial" });
  await page.getByLabel("Referans, gönderen veya sipariş ara").fill("sonuç-yok");
  await page.getByRole("button", { name: "Uygula" }).click();
  await expect(page.getByRole("heading", { name: "Filtrelere uyan iletişim mesajı bulunamadı" })).toBeVisible();
  await page.goto("/contact-messages?search=CM-20260821&pageNumber=1");
  await page.getByRole("link", { name: "CM-20260821-0001", exact: true }).click();
  await expect(page.getByText("Girilen numara yalnız kullanıcı beyanıdır", { exact: false })).toBeVisible();
  await expect(page.getByRole("link", { name: /Doğrulanmış siparişi aç/ })).toHaveCount(0);
  await expect(page.getByText("Teslim edilemedi", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: /retry|yeniden dene/i })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /İletişim mesajlarına dön/ })).toHaveAttribute("href", /search=CM-20260821/);
  await page.screenshot({ path: testInfo.outputPath("detail-desktop.png"), fullPage: true, caret: "initial" });
});

test("updates assignment, appends note and queues one reply on double click", async ({ page }, testInfo) => {
  await page.goto(`/contact-messages/${messageId}`);
  const assignment = page.getByLabel("Yönetici");
  const assignmentTarget = await assignment.inputValue() === "U00002" ? "U00001" : "U00002";
  await assignment.selectOption(assignmentTarget);
  await page.getByRole("button", { name: "Atamayı kaydet" }).click();
  await expect(page.getByText("Mesaj yöneticiye atandı.")).toBeVisible();
  await expect(page.getByRole("button", { name: "Atamayı kaydet" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Kaydediliyor…" })).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "Atama değiştirildi" })).toBeVisible();
  const noteText = `Kontrollü dahili test notu · ${testInfo.project.name}`;
  await page.getByRole("textbox", { name: "Not", exact: true }).fill(noteText);
  await page.getByRole("button", { name: "Notu activity'ye ekle" }).click();
  await expect(page.getByText(noteText, { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Notu activity'ye ekle" })).toBeVisible();
  await page.getByLabel("Yanıt metni").fill("Kontrollü müşteri test yanıtı");
  const before = await (await page.request.get("http://127.0.0.1:4310/__test/state")).json();
  const replyButton = page.getByRole("button", { name: "Yanıtı sıraya al" });
  await replyButton.evaluate((button: HTMLButtonElement) => { button.click(); button.click(); });
  await expect(page.getByText("Yanıt gönderim sırasına alındı.")).toBeVisible();
  await expect(page.getByRole("button", { name: "Yanıtı sıraya al" })).toBeVisible();
  const state = await (await page.request.get("http://127.0.0.1:4310/__test/state")).json();
  expect(state.replyRequests - before.replyRequests).toBe(1);
  await page.screenshot({ path: testInfo.outputPath("note-reply-state.png"), fullPage: true, caret: "initial" });
});

test("applies the authoritative reply status transition and refreshed concurrency state", async ({ page }) => {
  await page.request.post("http://127.0.0.1:4310/__test/prepare-unanswered");
  await page.goto(`/contact-messages/${messageId}`);
  await expect(page.getByText("İşlemde", { exact: true }).first()).toBeVisible();

  await page.getByLabel("Yanıt metni").fill("Durum geçişini doğrulayan kontrollü yanıt");
  await page.getByRole("button", { name: "Yanıtı sıraya al" }).click();

  await expect(page.getByText("Yanıt gönderim sırasına alındı.")).toBeVisible();
  await expect(page.getByText("Müşteri bekleniyor", { exact: true }).first()).toBeVisible();
  await expect(page.getByText("InProgress → WaitingForCustomer", { exact: true })).toBeVisible();

  const state = await (await page.request.get("http://127.0.0.1:4310/__test/state")).json();
  expect(state).toMatchObject({
    replyRequests: 1,
    replyCount: 1,
    status: 2,
    lastStatusActivity: { previousValue: "InProgress", newValue: "WaitingForCustomer" },
  });
  expect(state.firstRespondedAt).toEqual(expect.any(String));
});

test("preserves status decision boundary after concurrency conflict", async ({ page }, testInfo) => {
  await page.goto(`/contact-messages/${messageId}`);
  await page.request.post("http://127.0.0.1:4310/__test/status-conflict");
  await page.getByLabel("Yeni durum").selectOption("3");
  await page.getByRole("button", { name: "Durumu güncelle" }).click();
  await expect(page.getByText("Kayıt başka bir yönetici tarafından değiştirildi", { exact: false })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath("conflict-state.png"), fullPage: true, caret: "initial" });
  await page.getByRole("button", { name: "Güncel durumu kullan" }).click();
  await expect(page.getByText("Şu an:")).toBeVisible();
  await page.getByLabel("Yeni durum").selectOption("3");
  await page.getByRole("button", { name: "Durumu güncelle" }).click();
  await expect(page.getByText("Mesaj durumu güncellendi.")).toBeVisible();
  await expect(page.getByRole("button", { name: "Durumu güncelle" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Kaydediliyor…" })).toHaveCount(0);
});

test("shows Retry-After and keeps reply draft", async ({ page }, testInfo) => {
  await page.goto(`/contact-messages/${messageId}`);
  await page.request.post("http://127.0.0.1:4310/__test/reply-error", { data: { status: 429, code: "rate_limited", retryAfter: "45" } });
  const input = page.getByLabel("Yanıt metni");
  await input.fill("Retry sırasında korunacak taslak");
  await page.getByRole("button", { name: "Yanıtı sıraya al" }).click();
  await expect(page.getByRole("alert").filter({ hasText: "Çok fazla istek" })).toContainText("Retry-After: 45");
  await expect(input).toHaveValue("Retry sırasında korunacak taslak");
  await page.screenshot({ path: testInfo.outputPath("reply-error-state.png"), fullPage: true, caret: "initial" });
});

test("renders safe 400, 403, 404, 429, 500 and non-JSON read states", async ({ page }, testInfo) => {
  await page.goto("/contact-messages?search=fixture-400");
  await expect(page.getByRole("heading", { name: "İletişim mesajı verisi alınamadı" })).toBeVisible();
  await page.goto("/contact-messages?search=fixture-403");
  await expect(page.getByRole("heading", { name: "Bu alan için yetkiniz yok" })).toBeVisible();
  await page.goto(`/contact-messages/not-a-uuid`);
  await expect(page.getByRole("heading", { name: "İletişim mesajı bulunamadı" })).toBeVisible();
  await page.goto("/contact-messages?search=fixture-429");
  await expect(page.getByText("API 60 sonra yeniden denemenizi istiyor.")).toBeVisible();
  await page.goto("/contact-messages?search=fixture-500");
  await expect(page.getByText("İz: fixture-trace")).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath("read-error-state.png"), fullPage: true, caret: "initial" });
  await page.goto("/contact-messages?search=fixture-non-json");
  await expect(page.getByRole("heading", { name: "İletişim mesajı verisi alınamadı" })).toBeVisible();
  await expect(page.getByText("fixture upstream text")).toHaveCount(0);
});

test("renders the streamed loading state", async ({ page }, testInfo) => {
  const navigation = page.goto("/contact-messages?search=fixture-delay");
  await expect(page.getByLabel("İletişim mesajları yükleniyor")).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath("loading-state.png"), fullPage: true, caret: "initial" });
  await navigation;
  await expect(page.getByRole("heading", { name: "Filtrelere uyan iletişim mesajı bulunamadı" })).toBeVisible();
});

test("supports keyboard-only status, assignment, note and reply operations", async ({ page }, testInfo) => {
  await page.goto(`/contact-messages/${messageId}`);
  const statusSelect = page.getByLabel("Yeni durum");
  await statusSelect.focus();
  await page.keyboard.press("ArrowDown");
  await page.keyboard.press("Tab");
  await expect(page.getByRole("button", { name: "Durumu güncelle" })).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.getByText("Mesaj durumu güncellendi.")).toBeVisible();
  await expect(page.getByText("Mesaj durumu güncellendi.")).toBeHidden();

  const assignment = page.getByLabel("Yönetici");
  await assignment.focus();
  await page.keyboard.press("ArrowDown");
  await page.keyboard.press("Tab");
  await expect(page.getByRole("button", { name: "Atamayı kaydet" })).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.getByRole("heading", { name: "Atama değiştirildi" })).toBeVisible();

  const note = page.getByRole("textbox", { name: "Not", exact: true });
  await note.focus();
  await page.keyboard.type(`Klavye notu · ${testInfo.project.name}`);
  await page.keyboard.press("Tab");
  await expect(page.getByRole("button", { name: "Notu activity'ye ekle" })).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.getByText(`Klavye notu · ${testInfo.project.name}`, { exact: true })).toBeVisible();

  const reply = page.getByLabel("Yanıt metni");
  await reply.focus();
  await page.keyboard.type("Klavye ile kontrollü yanıt");
  await page.keyboard.press("Tab");
  await expect(page.getByRole("button", { name: "Yanıtı sıraya al" })).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.getByText("Yanıt gönderim sırasına alındı.")).toBeVisible();
});

test("keeps critical content usable at narrow viewport", async ({ page }, testInfo) => {
  await page.goto("/contact-messages");
  await expect(page.getByRole("link", { name: "CM-20260821-0001", exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: /mesajını aç/ })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath("list-mobile.png"), fullPage: true, caret: "initial" });
  await page.goto(`/contact-messages/${messageId}`);
  await expect(page.getByRole("heading", { name: "Orijinal mesaj" })).toBeVisible();
  await page.getByRole("textbox", { name: "Not", exact: true }).focus();
  await expect(page.getByRole("textbox", { name: "Not", exact: true })).toBeFocused();
  await page.screenshot({ path: testInfo.outputPath("detail-mobile.png"), fullPage: true, caret: "initial" });
});

function collectRuntimeErrors(page: Page) {
  const errors: string[] = [];
  runtimeErrors.set(page, errors);
  page.on("console", (message) => { if (message.type() === "error") errors.push(message.text()); });
  page.on("pageerror", (error) => errors.push(error.message));
}
