import type { PaymentDraft, PaymentInput } from "./types";

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
type ParseResult = { ok: true; input: PaymentInput; draft: PaymentDraft } | { ok: false; state: { status: "error"; message: string; fieldErrors: Record<string, string[]>; draft: PaymentDraft } };
const text = (data: FormData, name: string) => typeof data.get(name) === "string" ? String(data.get(name)).trim() : "";

export function parsePaymentForm(data: FormData): ParseResult {
  const allocationIds = data.getAll("allocationId").filter((value): value is string => typeof value === "string");
  const allocations = Object.fromEntries(allocationIds.map((id) => [id, text(data, `allocation:${id}`)]));
  const accountChoice = text(data, "accountChoice");
  const [choiceKind, choiceId] = accountChoice.split(":", 2);
  const draft: PaymentDraft = { idempotencyKey: text(data, "idempotencyKey"), currentAccountId: text(data, "currentAccountId"), type: text(data, "type"), amount: text(data, "amount"), paymentDate: text(data, "paymentDate"), accountKind: choiceKind === "bank" ? "bank" : "cash", financialAccountId: choiceId ?? "", referenceNumber: text(data, "referenceNumber"), description: text(data, "description"), allocations };
  const errors: Record<string, string[]> = {};
  const add = (key: string, message: string) => { (errors[key] ??= []).push(message); };
  const type = Number(draft.type);
  const amount = Number(draft.amount.replace(",", "."));
  if (!/^[A-Za-z0-9_-]{1,100}$/.test(draft.idempotencyKey)) add("idempotencyKey", "İşlem anahtarı geçersiz; sayfayı yenileyin.");
  if (!UUID.test(draft.currentAccountId)) add("currentAccountId", "Geçerli bir cari hesap seçin.");
  if (type !== 1 && type !== 2) add("type", "Geçerli bir işlem türü seçin.");
  if (!/^\d{1,16}(?:[.,]\d{1,2})?$/.test(draft.amount) || !Number.isFinite(amount) || amount <= 0) add("amount", "Tutar sıfırdan büyük, en fazla 2 ondalıklı ve 18 basamak sınırında olmalıdır.");
  if (!draft.paymentDate || Number.isNaN(Date.parse(`${draft.paymentDate}T00:00:00Z`))) add("paymentDate", "Geçerli bir işlem tarihi girin.");
  if (!UUID.test(draft.financialAccountId)) add("financialAccountId", "Aktif bir kasa veya banka hesabı seçin.");
  if (draft.referenceNumber.length > 100) add("referenceNumber", "Referans en fazla 100 karakter olabilir.");
  if (draft.description.length > 500) add("description", "Açıklama en fazla 500 karakter olabilir.");
  const parsedAllocations = allocationIds.flatMap((id) => {
    const raw = allocations[id]; if (!raw) return [];
    const value = Number(raw.replace(",", "."));
    if (!/^\d{1,16}(?:[.,]\d{1,2})?$/.test(raw) || !Number.isFinite(value) || value <= 0) { add(`allocation:${id}`, "Dağıtım tutarı sıfırdan büyük ve en fazla 2 ondalıklı olmalıdır."); return []; }
    return [{ currentAccountTransactionId: id, amount: value }];
  });
  if (new Set(allocationIds).size !== allocationIds.length || parsedAllocations.some((item) => !UUID.test(item.currentAccountTransactionId))) add("allocations", "Dağıtım hedefleri geçersiz veya tekrarlı.");
  const allocatedCents = parsedAllocations.reduce((sum, item) => sum + Math.round(item.amount * 100), 0);
  const amountCents = Math.round(amount * 100);
  if (type === 1 && parsedAllocations.length === 0) add("allocations", "Müşteri tahsilatı en az bir açık alacağa dağıtılmalıdır.");
  if (parsedAllocations.length > 0 && Number.isFinite(amount) && allocatedCents !== amountCents) add("allocations", "Dağıtım toplamı işlem tutarına kuruş düzeyinde eşit olmalıdır.");
  if (Object.keys(errors).length) return { ok: false, state: { status: "error", message: "İşlem kaydedilmedi. İşaretli alanları düzeltin.", fieldErrors: errors, draft } };
  return { ok: true, draft, input: { currentAccountId: draft.currentAccountId, type: type as 1 | 2, amount, paymentDate: `${draft.paymentDate}T00:00:00.000Z`, allocations: parsedAllocations, cashAccountId: draft.accountKind === "cash" ? draft.financialAccountId : null, bankAccountId: draft.accountKind === "bank" ? draft.financialAccountId : null, currencyCode: "TRY", exchangeRate: 1, referenceNumber: draft.referenceNumber || null, description: draft.description || null } };
}
