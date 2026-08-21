import http from "node:http";
import { randomUUID } from "node:crypto";

const messageId = "11111111-1111-4111-8111-111111111111";
const now = "2026-08-21T10:00:00.000Z";
const admins = [
  { id: "U00001", email: "admin.fixture@example.test", firstName: "Ada", lastName: "Yönetici", phoneNumber: null, role: 2, status: 1, lastLoginAt: now, passwordChangedAt: null, createdAt: now, updatedAt: now, orderCount: 0 },
  { id: "U00002", email: "operasyon.fixture@example.test", firstName: "Uzun Türkçe İsimli", lastName: "Operasyon Yöneticisi", phoneNumber: null, role: 2, status: 1, lastLoginAt: now, passwordChangedAt: null, createdAt: now, updatedAt: now, orderCount: 0 },
];
const detail = {
  id: messageId,
  referenceNumber: "CM-20260821-0001",
  userId: null,
  name: "Çok Uzun Türkçe İsimli Test Müşterisi",
  email: "uzun.fixture.adresi@example.test",
  phone: "+90 555 000 00 00",
  subject: 0,
  providedOrderNumber: "ORD-BEYAN-0001",
  verifiedOrderId: null,
  isOrderVerified: false,
  message: `${"Bu yalnız kontrollü test mesajıdır. ".repeat(140)}\nİkinci satır.`,
  status: 2,
  assignedAdminUserId: null,
  createdAt: now,
  updatedAt: now,
  firstRespondedAt: "2026-08-21T10:02:00.000Z",
  resolvedAt: null,
  closedAt: null,
  concurrencyToken: "22222222-2222-4222-8222-222222222222",
  privacyNoticeVersion: "2026-08",
  privacyNoticePublishedAt: now,
  activities: [
    { id: "33333333-3333-4333-8333-333333333331", type: 0, actorAdminUserId: null, content: null, previousValue: null, newValue: null, replyId: null, createdAt: now },
    { id: "33333333-3333-4333-8333-333333333332", type: 4, actorAdminUserId: "U00001", content: null, previousValue: null, newValue: null, replyId: "44444444-4444-4444-8444-444444444444", createdAt: "2026-08-21T10:02:00.000Z" },
    { id: "33333333-3333-4333-8333-333333333333", type: 1, actorAdminUserId: "U00001", content: null, previousValue: "New", newValue: "WaitingForCustomer", replyId: null, createdAt: "2026-08-21T10:02:00.000Z" },
  ],
  replies: [
    { id: "44444444-4444-4444-8444-444444444444", adminUserId: "U00001", body: "Kontrollü test yanıtı teslim edilemedi; UI retry aksiyonu göstermemeli.", deliveryStatus: 3, createdAt: "2026-08-21T10:02:00.000Z" },
  ],
};
const baselineDetail = structuredClone(detail);
let replyRequests = 0;
let nextReplyError = null;
let nextStatusConflict = false;

const server = http.createServer(async (request, response) => {
  const url = new URL(request.url || "/", "http://127.0.0.1:4310");
  if (url.pathname === "/__test/reset" && request.method === "POST") {
    Object.assign(detail, structuredClone(baselineDetail));
    replyRequests = 0;
    nextReplyError = null;
    nextStatusConflict = false;
    return json(response, 204);
  }
  if (url.pathname === "/__test/state") {
    const statusActivities = detail.activities.filter((entry) => entry.type === 1);
    const lastStatusActivity = statusActivities.at(-1);
    return json(response, 200, {
      replyRequests,
      replyCount: detail.replies.length,
      status: detail.status,
      firstRespondedAt: detail.firstRespondedAt,
      lastStatusActivity: lastStatusActivity
        ? { previousValue: lastStatusActivity.previousValue, newValue: lastStatusActivity.newValue }
        : null,
    });
  }
  if (url.pathname === "/__test/prepare-unanswered" && request.method === "POST") {
    Object.assign(detail, structuredClone(baselineDetail), {
      status: 1,
      firstRespondedAt: null,
      activities: structuredClone(baselineDetail.activities.filter((entry) => entry.type === 0)),
      replies: [],
    });
    replyRequests = 0;
    nextReplyError = null;
    nextStatusConflict = false;
    touch();
    return json(response, 204);
  }
  if (url.pathname === "/__test/status-conflict" && request.method === "POST") {
    nextStatusConflict = true;
    return json(response, 204);
  }
  if (url.pathname === "/__test/reply-error" && request.method === "POST") {
    nextReplyError = await body(request);
    return json(response, 204);
  }
  if (url.pathname === "/api/users/me") return json(response, 200, admins[0]);
  if (url.pathname === "/api/store-settings/admin") return json(response, 200, { displayName: "ELEVEN Test Mağazası" });
  if (url.pathname === "/api/users") return json(response, 200, page(admins));
  if (url.pathname === "/api/contact-messages" && request.method === "GET") {
    const search = url.searchParams.get("Search")?.toLocaleLowerCase("tr-TR");
    if (search === "fixture-400") return problem(response, 400, "validation_error", "Kontrollü doğrulama hatası.");
    if (search === "fixture-403") return problem(response, 403, "forbidden", "Kontrollü yetki hatası.");
    if (search === "fixture-429") { response.setHeader("Retry-After", "60"); return problem(response, 429, "rate_limited", "Kontrollü limit hatası."); }
    if (search === "fixture-500") return problem(response, 500, "internal_error", "Kontrollü servis hatası.");
    if (search === "fixture-non-json") { response.writeHead(502, { "Content-Type": "text/plain" }); response.end("fixture upstream text"); return; }
    if (search === "fixture-delay") await new Promise((resolve) => setTimeout(resolve, 1_500));
    const summary = summaryFromDetail();
    const items = search && !`${summary.referenceNumber} ${summary.name} ${summary.email}`.toLocaleLowerCase("tr-TR").includes(search) ? [] : [summary];
    const pageNumber = Number(url.searchParams.get("PageNumber") || "1");
    return json(response, 200, { ...page(items), pageNumber, totalCount: items.length ? 2 : 0, totalPages: items.length ? 2 : 0, hasPreviousPage: pageNumber > 1, hasNextPage: items.length > 0 && pageNumber < 2 });
  }
  if (url.pathname === `/api/contact-messages/${messageId}` && request.method === "GET") return json(response, 200, detail);
  if (url.pathname === `/api/contact-messages/${messageId}/status` && request.method === "PATCH") {
    const input = await body(request);
    if (nextStatusConflict) {
      nextStatusConflict = false;
      const previousStatus = detail.status;
      detail.status = 1;
      detail.activities.push(activity(1, statusName(previousStatus), "InProgress"));
      touch();
      return problem(response, 409, "concurrency_conflict", "Kayıt değişti.");
    }
    detail.activities.push(activity(1, statusName(detail.status), statusName(input.status)));
    detail.status = input.status;
    touch();
    return json(response, 200, detail);
  }
  if (url.pathname === `/api/contact-messages/${messageId}/assignment` && request.method === "PATCH") {
    const input = await body(request);
    detail.activities.push(activity(2, detail.assignedAdminUserId, input.assignedAdminUserId));
    detail.assignedAdminUserId = input.assignedAdminUserId;
    touch();
    return json(response, 200, detail);
  }
  if (url.pathname === `/api/contact-messages/${messageId}/notes` && request.method === "POST") {
    const input = await body(request);
    detail.activities.push({ ...activity(3), content: input.note });
    touch();
    return json(response, 200, detail);
  }
  if (url.pathname === `/api/contact-messages/${messageId}/replies` && request.method === "POST") {
    replyRequests += 1;
    if (nextReplyError) {
      const error = nextReplyError;
      nextReplyError = null;
      response.setHeader("Retry-After", error.retryAfter || "30");
      return problem(response, error.status || 429, error.code || "rate_limited", "Kontrollü hata.");
    }
    const input = await body(request);
    const reply = { id: randomUUID(), adminUserId: "U00001", body: input.body, deliveryStatus: 0, createdAt: new Date().toISOString() };
    detail.replies.push(reply);
    detail.activities.push({ ...activity(4), replyId: reply.id });
    detail.firstRespondedAt ||= reply.createdAt;
    if (detail.status === 0 || detail.status === 1) {
      const previousStatus = detail.status;
      detail.status = 2;
      detail.activities.push(activity(1, statusName(previousStatus), "WaitingForCustomer"));
    }
    touch();
    return json(response, 202, detail);
  }
  return problem(response, 404, "not_found", "Fixture route bulunamadı.");
});

server.listen(4310, "127.0.0.1");

// Burada mutable detail fixture'ını liste endpointinin özet projection'ına dönüştürüyorum.
function summaryFromDetail() {
  return { id: detail.id, referenceNumber: detail.referenceNumber, name: detail.name, email: detail.email, subject: detail.subject, status: detail.status, providedOrderNumber: detail.providedOrderNumber, hasVerifiedOrder: detail.isOrderVerified, assignedAdminUserId: detail.assignedAdminUserId, createdAt: detail.createdAt, updatedAt: detail.updatedAt };
}
// Burada fixture koleksiyonunu sözleşmedeki paged result biçimine sarıyorum.
function page(items) { return { items, pageNumber: 1, pageSize: 20, totalCount: items.length, totalPages: items.length ? 1 : 0, hasPreviousPage: false, hasNextPage: false }; }
// Burada test mutasyonlarının append-only audit girdisini üretiyorum.
function activity(type, previousValue = null, newValue = null) { return { id: randomUUID(), type, actorAdminUserId: "U00001", content: null, previousValue, newValue, replyId: null, createdAt: new Date().toISOString() }; }
// Burada status audit alanlarını API'nin numeric değer değil enum adı döndürme sözleşmesiyle eşliyorum.
function statusName(status) { return ["New", "InProgress", "WaitingForCustomer", "Resolved", "Closed", "Spam"][status] ?? "Unknown"; }
// Burada her fixture mutasyonundan sonra authoritative concurrency snapshot'ını değiştiriyorum.
function touch() { detail.updatedAt = new Date().toISOString(); detail.concurrencyToken = randomUUID(); }
// Burada yalnız kontrollü JSON test istek gövdelerini parse ediyorum.
async function body(request) { const chunks = []; for await (const chunk of request) chunks.push(chunk); return JSON.parse(Buffer.concat(chunks).toString("utf8") || "{}"); }
// Burada mock endpoint JSON cevaplarını tek içerik tipiyle gönderiyorum.
function json(response, status, value) { response.writeHead(status, { "Content-Type": "application/json" }); response.end(status === 204 ? undefined : JSON.stringify(value)); }
// Burada güvenli ProblemDetails fixture'ını gerçek hata code/status alanlarıyla üretiyorum.
function problem(response, status, code, detailText) { return json(response, status, { title: "Kontrollü test hatası", status, code, detail: detailText, traceId: "fixture-trace" }); }
