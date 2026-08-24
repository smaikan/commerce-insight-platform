import type {
  AccountingFormState,
  ExpenseCategoryDraft,
  GeneralExpenseDraft,
  PurchaseInvoiceAllocationDraft,
  PurchaseInvoiceAllocationInput,
  PurchaseInvoiceExpenseDraft,
  PurchaseInvoiceExpenseInput,
  PurchaseInvoiceFormDraft,
  PurchaseInvoiceHeaderInput,
  PurchaseInvoiceLineDraft,
  PurchaseInvoiceLineInput,
} from "./types";

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const DATE = /^\d{4}-\d{2}-\d{2}$/;

// Burada belge formunun ham alanlarını kayıpsız taslağa çevirip API input sınırlarını doğruluyorum.
export function parsePurchaseInvoiceForm(formData: FormData):
  | { ok: true; header: PurchaseInvoiceHeaderInput; lines: PurchaseInvoiceLineInput[]; draft: PurchaseInvoiceFormDraft }
  | { ok: false; state: AccountingFormState<PurchaseInvoiceFormDraft> } {
  const draft = purchaseInvoiceDraft(formData);
  const errors: Record<string, string[]> = {};
  required(errors, "currentAccountId", draft.currentAccountId, "Aktif bir tedarikçi seçin.");
  if (draft.currentAccountId && !UUID.test(draft.currentAccountId)) errors.currentAccountId = ["Tedarikçi kimliği geçerli değil."];
  length(errors, "invoiceNumber", draft.invoiceNumber, 1, 100, "Fatura numarası");
  validDate(errors, "invoiceDate", draft.invoiceDate, "Fatura tarihi");
  if (draft.dueDate) validDate(errors, "dueDate", draft.dueDate, "Vade tarihi");
  if (draft.description.length > 500) errors.description = ["Açıklama en fazla 500 karakter olabilir."];
  if (draft.lines.length === 0) errors.lines = ["Faturada en az bir satır bulunmalıdır."];

  const seenLineNumbers = new Set<number>();
  const lines: PurchaseInvoiceLineInput[] = [];
  draft.lines.forEach((line, index) => {
    const prefix = `lines.${index}`;
    const lineNumber = integer(line.lineNumber);
    const quantity = decimal(line.purchaseQuantity);
    const units = decimal(line.unitsPerPurchaseUnit);
    const vatRate = decimal(line.vatRate);
    const enteredPrice = decimal(line.enteredUnitPrice);
    const priceEntryMode = integer(line.priceEntryMode);
    if (!lineNumber || lineNumber < 1) errors[`${prefix}.lineNumber`] = ["Satır numarası pozitif tam sayı olmalıdır."];
    else if (seenLineNumbers.has(lineNumber)) errors[`${prefix}.lineNumber`] = ["Satır numaraları benzersiz olmalıdır."];
    else seenLineNumbers.add(lineNumber);
    if (!UUID.test(line.productVariantId)) errors[`${prefix}.productVariantId`] = ["Bir ürün varyantı seçin."];
    if (quantity === null || quantity <= 0) errors[`${prefix}.purchaseQuantity`] = ["Alış miktarı sıfırdan büyük olmalıdır."];
    if (units === null || units <= 0) errors[`${prefix}.unitsPerPurchaseUnit`] = ["Birim katsayısı sıfırdan büyük olmalıdır."];
    if (quantity !== null && units !== null && (!Number.isInteger(quantity * units) || quantity * units <= 0)) errors[`${prefix}.unitsPerPurchaseUnit`] = ["Alış miktarı × birim katsayısı pozitif tam sayı stok miktarı üretmelidir."];
    length(errors, `${prefix}.unitOfMeasure`, line.unitOfMeasure, 1, 50, "Ölçü birimi");
    if (priceEntryMode !== 1 && priceEntryMode !== 2) errors[`${prefix}.priceEntryMode`] = ["Geçerli bir fiyat giriş şekli seçin."];
    if (vatRate === null || vatRate < 0 || vatRate > 100) errors[`${prefix}.vatRate`] = ["KDV oranı 0–100 arasında olmalıdır."];
    if (enteredPrice === null || enteredPrice < 0) errors[`${prefix}.enteredUnitPrice`] = ["Birim fiyat sıfır veya daha büyük olmalıdır."];
    if (!errors[`${prefix}.lineNumber`] && !errors[`${prefix}.productVariantId`] && !errors[`${prefix}.purchaseQuantity`] && !errors[`${prefix}.unitsPerPurchaseUnit`] && !errors[`${prefix}.unitOfMeasure`] && !errors[`${prefix}.priceEntryMode`] && !errors[`${prefix}.vatRate`] && !errors[`${prefix}.enteredUnitPrice`]) {
      lines.push({
        lineNumber: lineNumber!, productVariantId: line.productVariantId, purchaseQuantity: quantity!, unitOfMeasure: line.unitOfMeasure,
        unitsPerPurchaseUnit: units!, priceEntryMode: priceEntryMode as 1 | 2, vatRate: vatRate!, enteredUnitPrice: enteredPrice!,
        isInvoiceDiscountEligible: line.isInvoiceDiscountEligible,
      });
    }
  });

  if (Object.keys(errors).length) return { ok: false, state: { status: "error", message: "İşaretli belge ve satır alanlarını kontrol edin.", fieldErrors: errors, draft } };
  return {
    ok: true,
    header: {
      currentAccountId: draft.currentAccountId,
      invoiceNumber: draft.invoiceNumber,
      invoiceDate: toIsoDate(draft.invoiceDate),
      dueDate: draft.dueDate ? toIsoDate(draft.dueDate) : null,
      currencyCode: "TRY",
      exchangeRate: 1,
      description: draft.description || null,
    },
    lines,
    draft,
  };
}

export function purchaseInvoiceDraft(formData: FormData): PurchaseInvoiceFormDraft {
  const parsedLines = parseJson<unknown[]>(text(formData, "linesJson"), []);
  return {
    currentAccountId: text(formData, "currentAccountId"), invoiceNumber: text(formData, "invoiceNumber"),
    invoiceDate: text(formData, "invoiceDate"), dueDate: text(formData, "dueDate"), description: text(formData, "description"),
    lines: parsedLines.map((value, index) => normalizeLineDraft(value, index)),
  };
}

// Burada stok tahsis editörünün yalnız benzersiz pozitif hareket miktarlarını API'ye geçirmesini sağlıyorum.
export function parseAllocationForm(formData: FormData):
  | { ok: true; allocations: PurchaseInvoiceAllocationInput[]; draft: PurchaseInvoiceAllocationDraft }
  | { ok: false; state: AccountingFormState<PurchaseInvoiceAllocationDraft> } {
  const raw = parseJson<unknown[]>(text(formData, "allocationsJson"), []);
  const draft: PurchaseInvoiceAllocationDraft = { allocations: raw.map((item) => {
    const record = object(item);
    return { stockMovementId: string(record.stockMovementId), quantity: string(record.quantity) };
  }) };
  const errors: Record<string, string[]> = {};
  const seen = new Set<string>();
  const allocations = draft.allocations.flatMap((item, index) => {
    const quantity = integer(item.quantity);
    if (!UUID.test(item.stockMovementId)) errors[`allocations.${index}.stockMovementId`] = ["Stok hareketi kimliği geçerli değil."];
    if (seen.has(item.stockMovementId)) errors[`allocations.${index}.stockMovementId`] = ["Aynı stok hareketi yalnız bir kez tahsis edilebilir."];
    seen.add(item.stockMovementId);
    if (!quantity || quantity <= 0) errors[`allocations.${index}.quantity`] = ["Tahsis miktarı pozitif tam sayı olmalıdır."];
    return UUID.test(item.stockMovementId) && quantity && quantity > 0 ? [{ stockMovementId: item.stockMovementId, quantity }] : [];
  });
  if (allocations.length === 0) errors.allocations = ["En az bir pozitif stok hareketi tahsisi girin."];
  if (Object.keys(errors).length) return { ok: false, state: { status: "error", message: "Tahsis miktarlarını kontrol edin.", fieldErrors: errors, draft } };
  return { ok: true, allocations, draft };
}

// Burada fatura giderini otomatik ve manuel dağıtım kurallarına göre doğruluyorum.
export function parsePurchaseInvoiceExpenseForm(formData: FormData, lineIds: string[]):
  | { ok: true; input: PurchaseInvoiceExpenseInput; draft: PurchaseInvoiceExpenseDraft }
  | { ok: false; state: AccountingFormState<PurchaseInvoiceExpenseDraft> } {
  const rawManual = parseJson<unknown[]>(text(formData, "manualAllocationsJson"), []);
  const draft: PurchaseInvoiceExpenseDraft = {
    categoryId: text(formData, "categoryId"), allocationMethod: text(formData, "allocationMethod"),
    amountExcludingVat: text(formData, "amountExcludingVat"), vatRate: text(formData, "vatRate"), description: text(formData, "description"),
    manualAllocations: rawManual.map((item) => { const record = object(item); return { purchaseInvoiceLineId: string(record.purchaseInvoiceLineId), amountExcludingVat: string(record.amountExcludingVat) }; }),
  };
  const errors: Record<string, string[]> = {};
  const method = integer(draft.allocationMethod);
  const amount = decimal(draft.amountExcludingVat);
  const vatRate = decimal(draft.vatRate);
  if (!UUID.test(draft.categoryId)) errors.categoryId = ["Aktif bir gider kategorisi seçin."];
  if (method !== 1 && method !== 2 && method !== 3) errors.allocationMethod = ["Geçerli bir dağıtım yöntemi seçin."];
  if (amount === null || amount <= 0) errors.amountExcludingVat = ["KDV hariç gider tutarı sıfırdan büyük olmalıdır."];
  if (vatRate === null || vatRate < 0 || vatRate > 100) errors.vatRate = ["KDV oranı 0–100 arasında olmalıdır."];
  if (draft.description.length > 500) errors.description = ["Açıklama en fazla 500 karakter olabilir."];

  let manualAllocations: PurchaseInvoiceExpenseInput["manualAllocations"] = null;
  if (method === 3) {
    const expected = new Set(lineIds);
    const actual = new Set(draft.manualAllocations.map((item) => item.purchaseInvoiceLineId));
    if (draft.manualAllocations.length !== lineIds.length || actual.size !== lineIds.length || [...expected].some((id) => !actual.has(id))) errors.manualAllocations = ["Manuel dağıtım her fatura satırını tam bir kez içermelidir."];
    manualAllocations = draft.manualAllocations.flatMap((item, index) => {
      const value = decimal(item.amountExcludingVat);
      if (value === null || value < 0) errors[`manualAllocations.${index}.amountExcludingVat`] = ["Dağıtım tutarı negatif olamaz."];
      return value !== null && value >= 0 ? [{ purchaseInvoiceLineId: item.purchaseInvoiceLineId, amountExcludingVat: value }] : [];
    });
    const total = manualAllocations.reduce((sum, item) => sum + item.amountExcludingVat, 0);
    if (amount !== null && round2(total) !== round2(amount)) errors.manualAllocations = ["Manuel dağıtım toplamı gider tutarına eşit olmalıdır."];
  }
  if (Object.keys(errors).length) return { ok: false, state: { status: "error", message: "Gider dağıtımı alanlarını kontrol edin.", fieldErrors: errors, draft } };
  return { ok: true, input: { categoryId: draft.categoryId, allocationMethod: method as 1 | 2 | 3, amountExcludingVat: amount!, vatRate: vatRate!, description: draft.description || null, manualAllocations }, draft };
}

export function parseExpenseCategoryForm(formData: FormData): { ok: true; input: ExpenseCategoryDraft; draft: ExpenseCategoryDraft } | { ok: false; state: AccountingFormState<ExpenseCategoryDraft> } {
  const draft = { code: text(formData, "code"), name: text(formData, "name") };
  const errors: Record<string, string[]> = {};
  length(errors, "code", draft.code, 1, 50, "Kategori kodu");
  length(errors, "name", draft.name, 1, 150, "Kategori adı");
  return Object.keys(errors).length ? { ok: false, state: { status: "error", message: "Kategori alanlarını kontrol edin.", fieldErrors: errors, draft } } : { ok: true, input: draft, draft };
}

export function parseGeneralExpenseForm(formData: FormData): { ok: true; input: { categoryId: string; amountExcludingVat: number; vatRate: number; expenseDate: string; description: string }; draft: GeneralExpenseDraft } | { ok: false; state: AccountingFormState<GeneralExpenseDraft> } {
  const draft = { categoryId: text(formData, "categoryId"), amountExcludingVat: text(formData, "amountExcludingVat"), vatRate: text(formData, "vatRate"), expenseDate: text(formData, "expenseDate"), description: text(formData, "description") };
  const errors: Record<string, string[]> = {};
  const amount = decimal(draft.amountExcludingVat);
  const vatRate = decimal(draft.vatRate);
  if (!UUID.test(draft.categoryId)) errors.categoryId = ["Aktif bir gider kategorisi seçin."];
  if (amount === null || amount <= 0) errors.amountExcludingVat = ["KDV hariç tutar sıfırdan büyük olmalıdır."];
  if (vatRate === null || vatRate < 0 || vatRate > 100) errors.vatRate = ["KDV oranı 0–100 arasında olmalıdır."];
  validDate(errors, "expenseDate", draft.expenseDate, "Gider tarihi");
  length(errors, "description", draft.description, 1, 500, "Açıklama");
  return Object.keys(errors).length ? { ok: false, state: { status: "error", message: "Genel gider alanlarını kontrol edin.", fieldErrors: errors, draft } } : { ok: true, input: { categoryId: draft.categoryId, amountExcludingVat: amount!, vatRate: vatRate!, expenseDate: toIsoDate(draft.expenseDate), description: draft.description }, draft };
}

function normalizeLineDraft(value: unknown, index: number): PurchaseInvoiceLineDraft {
  const record = object(value);
  return {
    key: string(record.key) || `line-${index + 1}`, lineNumber: string(record.lineNumber), productVariantId: string(record.productVariantId),
    purchaseQuantity: string(record.purchaseQuantity), unitOfMeasure: string(record.unitOfMeasure), unitsPerPurchaseUnit: string(record.unitsPerPurchaseUnit),
    priceEntryMode: string(record.priceEntryMode), vatRate: string(record.vatRate), enteredUnitPrice: string(record.enteredUnitPrice),
    isInvoiceDiscountEligible: record.isInvoiceDiscountEligible !== false, hasAllocations: record.hasAllocations === true,
  };
}

function text(data: FormData, name: string): string { const value = data.get(name); return typeof value === "string" ? value.trim() : ""; }
function object(value: unknown): Record<string, unknown> { return value !== null && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {}; }
function string(value: unknown): string { return typeof value === "string" || typeof value === "number" ? String(value).trim() : ""; }
function decimal(value: string): number | null { const number = Number(value.replace(",", ".")); return value !== "" && Number.isFinite(number) ? number : null; }
function integer(value: string): number | null { const number = Number(value); return Number.isInteger(number) ? number : null; }
function parseJson<T>(value: string, fallback: T): T { try { return JSON.parse(value) as T; } catch { return fallback; } }
function round2(value: number): number { return Math.round((value + Number.EPSILON) * 100) / 100; }
function toIsoDate(value: string): string { return `${value}T00:00:00.000Z`; }
function required(errors: Record<string, string[]>, key: string, value: string, message: string): void { if (!value) errors[key] = [message]; }
function length(errors: Record<string, string[]>, key: string, value: string, minimum: number, maximum: number, label: string): void { if (value.length < minimum || value.length > maximum) errors[key] = [`${label} ${minimum}–${maximum} karakter olmalıdır.`]; }
function validDate(errors: Record<string, string[]>, key: string, value: string, label: string): void { if (!DATE.test(value) || Number.isNaN(new Date(`${value}T00:00:00Z`).getTime())) errors[key] = [`${label} geçerli bir tarih olmalıdır.`]; }
