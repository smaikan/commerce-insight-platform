import type {
  AccountingSalesOrderHeaderInput,
  AccountingSalesOrderLineInput,
  InvoiceFromOrderDraft,
  SalesFormState,
  SalesInvoiceEditDraft,
  SalesInvoiceHeaderInput,
  SalesLineDraft,
  SalesOrderFormDraft,
} from "./types";

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const KEY = /^[A-Za-z0-9_-]{1,100}$/;
const DATE = /^\d{4}-\d{2}-\d{2}$/;

export function parseSalesOrderForm(formData: FormData):
  | { ok: true; header: AccountingSalesOrderHeaderInput; lines: AccountingSalesOrderLineInput[]; invoice: SalesInvoiceHeaderInput | null; draft: SalesOrderFormDraft }
  | { ok: false; state: SalesFormState<SalesOrderFormDraft> } {
  const draft = salesOrderDraft(formData);
  const errors: Record<string, string[]> = {};
  if (!KEY.test(draft.idempotencyKey)) errors.idempotencyKey = ["İşlem anahtarı geçersiz. Sayfayı yenileyin."];
  if (!UUID.test(draft.currentAccountId)) errors.currentAccountId = ["Aktif bir müşteri cari hesabı seçin."];
  length(errors, "orderNumber", draft.orderNumber, 1, 100, "Satış numarası");
  validDate(errors, "orderDate", draft.orderDate, "Satış tarihi");
  optionalDate(errors, "dueDate", draft.dueDate, "Vade tarihi");
  length(errors, "description", draft.description, 0, 500, "Açıklama");
  const shippingTotal = decimal(draft.shippingTotal);
  const shippingPayer = integer(draft.shippingPayer);
  if (shippingTotal === null || shippingTotal < 0) errors.shippingTotal = ["Kargo tutarı sıfır veya daha büyük olmalıdır."];
  if (shippingPayer !== 0 && shippingPayer !== 1 && shippingPayer !== 2) errors.shippingPayer = ["Geçerli bir kargo ödeyeni seçin."];
  if (shippingTotal === 0 && shippingPayer !== 0) errors.shippingPayer = ["Kargo tutarı sıfırken ödeyen 'Yok' olmalıdır."];
  if (shippingTotal !== null && shippingTotal > 0 && shippingPayer === 0) errors.shippingPayer = ["Pozitif kargo tutarı için satıcı veya müşteri seçin."];
  const invoiceDiscount = parseDiscount(errors, "invoiceDiscount", draft.invoiceDiscountType, draft.invoiceDiscountValue, draft.invoiceDiscountTaxBasis, "", true);
  const lines = parseLines(draft.lines, errors);
  if (!draft.lines.length) errors.lines = ["Satışta en az bir satır bulunmalıdır."];

  let invoice: SalesInvoiceHeaderInput | null = null;
  if (draft.createInvoice) {
    invoice = parseInvoiceHeader(errors, draft.invoiceNumber, draft.invoiceDate, draft.invoiceDueDate, draft.invoiceDescription, "invoice");
  }

  if (Object.keys(errors).length) return { ok: false, state: { status: "error", message: "İşaretli satış belgesi alanlarını kontrol edin.", fieldErrors: errors, draft } };
  return {
    ok: true,
    header: {
      currentAccountId: draft.currentAccountId,
      orderNumber: draft.orderNumber,
      orderDate: toIsoDate(draft.orderDate),
      dueDate: draft.dueDate ? toIsoDate(draft.dueDate) : null,
      currencyCode: "TRY",
      exchangeRate: 1,
      shippingTotal: shippingTotal!,
      shippingPayer: shippingPayer as 0 | 1 | 2,
      description: draft.description || null,
      invoiceDiscountType: invoiceDiscount.type,
      invoiceDiscountValue: invoiceDiscount.value,
      invoiceDiscountTaxBasis: invoiceDiscount.taxBasis,
    },
    lines,
    invoice,
    draft,
  };
}

export function parseSalesInvoiceEditForm(formData: FormData):
  | { ok: true; header: SalesInvoiceHeaderInput; lines: AccountingSalesOrderLineInput[]; draft: SalesInvoiceEditDraft }
  | { ok: false; state: SalesFormState<SalesInvoiceEditDraft> } {
  const raw = parseJson<unknown[]>(text(formData, "linesJson"), []);
  const draft: SalesInvoiceEditDraft = {
    invoiceNumber: text(formData, "invoiceNumber"), invoiceDate: text(formData, "invoiceDate"), dueDate: text(formData, "dueDate"), description: text(formData, "description"),
    lines: raw.map(normalizeLine),
  };
  const errors: Record<string, string[]> = {};
  const header = parseInvoiceHeader(errors, draft.invoiceNumber, draft.invoiceDate, draft.dueDate, draft.description, "");
  const lines = parseLines(draft.lines, errors);
  if (!draft.lines.length) errors.lines = ["Faturada en az bir satır bulunmalıdır."];
  return Object.keys(errors).length
    ? { ok: false, state: { status: "error", message: "İşaretli fatura alanlarını kontrol edin.", fieldErrors: errors, draft } }
    : { ok: true, header, lines, draft };
}

export function parseInvoiceFromOrderForm(formData: FormData):
  | { ok: true; header: SalesInvoiceHeaderInput; draft: InvoiceFromOrderDraft }
  | { ok: false; state: SalesFormState<InvoiceFromOrderDraft> } {
  const draft = { invoiceNumber: text(formData, "invoiceNumber"), invoiceDate: text(formData, "invoiceDate"), dueDate: text(formData, "dueDate"), description: text(formData, "description") };
  const errors: Record<string, string[]> = {};
  const header = parseInvoiceHeader(errors, draft.invoiceNumber, draft.invoiceDate, draft.dueDate, draft.description, "");
  return Object.keys(errors).length
    ? { ok: false, state: { status: "error", message: "Fatura başlığı alanlarını kontrol edin.", fieldErrors: errors, draft } }
    : { ok: true, header, draft };
}

export function salesOrderDraft(formData: FormData): SalesOrderFormDraft {
  const raw = parseJson<unknown[]>(text(formData, "linesJson"), []);
  return {
    idempotencyKey: text(formData, "idempotencyKey"), currentAccountId: text(formData, "currentAccountId"), orderNumber: text(formData, "orderNumber"),
    orderDate: text(formData, "orderDate"), dueDate: text(formData, "dueDate"), shippingTotal: text(formData, "shippingTotal"), shippingPayer: text(formData, "shippingPayer"), description: text(formData, "description"),
    invoiceDiscountType: text(formData, "invoiceDiscountType"), invoiceDiscountValue: text(formData, "invoiceDiscountValue"), invoiceDiscountTaxBasis: text(formData, "invoiceDiscountTaxBasis"),
    createInvoice: formData.get("createInvoice") === "on", invoiceNumber: text(formData, "invoiceNumber"), invoiceDate: text(formData, "invoiceDate"), invoiceDueDate: text(formData, "invoiceDueDate"), invoiceDescription: text(formData, "invoiceDescription"),
    lines: raw.map(normalizeLine),
  };
}

function parseLines(drafts: SalesLineDraft[], errors: Record<string, string[]>): AccountingSalesOrderLineInput[] {
  const seen = new Set<number>();
  return drafts.flatMap((line, index) => {
    const prefix = `lines.${index}`;
    const lineNumber = integer(line.lineNumber);
    const quantity = decimal(line.quantity);
    const units = decimal(line.unitsPerSaleUnit);
    const priceMode = integer(line.priceEntryMode);
    const vatRate = decimal(line.vatRate);
    const price = decimal(line.enteredUnitPrice);
    if (!lineNumber || lineNumber < 1 || seen.has(lineNumber)) errors[`${prefix}.lineNumber`] = [seen.has(lineNumber ?? 0) ? "Satır numaraları benzersiz olmalıdır." : "Satır numarası pozitif tam sayı olmalıdır."];
    else seen.add(lineNumber);
    if (!UUID.test(line.productVariantId)) errors[`${prefix}.productVariantId`] = ["Bir ürün varyantı seçin."];
    if (quantity === null || quantity <= 0) errors[`${prefix}.quantity`] = ["Satış miktarı sıfırdan büyük olmalıdır."];
    if (units === null || units <= 0) errors[`${prefix}.unitsPerSaleUnit`] = ["Birim katsayısı sıfırdan büyük olmalıdır."];
    if (quantity !== null && units !== null && (!Number.isInteger(quantity * units) || quantity * units <= 0 || quantity * units > 2_147_483_647)) errors[`${prefix}.unitsPerSaleUnit`] = ["Satış miktarı × birim katsayısı 1–2.147.483.647 aralığında tam sayı stok miktarı üretmelidir."];
    length(errors, `${prefix}.unitOfMeasure`, line.unitOfMeasure, 1, 50, "Ölçü birimi");
    if (priceMode !== 1 && priceMode !== 2) errors[`${prefix}.priceEntryMode`] = ["Geçerli bir fiyat giriş şekli seçin."];
    if (vatRate === null || vatRate < 0 || vatRate > 100) errors[`${prefix}.vatRate`] = ["KDV oranı 0–100 arasında olmalıdır."];
    if (price === null || price < 0) errors[`${prefix}.enteredUnitPrice`] = ["Birim fiyat sıfır veya daha büyük olmalıdır."];
    const discount = parseDiscount(errors, `${prefix}.lineDiscount`, line.lineDiscountType, line.lineDiscountValue, line.lineDiscountTaxBasis, line.lineDiscountUnitBasis, false);
    const hasLineError = Object.keys(errors).some((key) => key.startsWith(prefix));
    return hasLineError ? [] : [{
      lineNumber: lineNumber!, productVariantId: line.productVariantId, quantity: quantity!, unitOfMeasure: line.unitOfMeasure, unitsPerSaleUnit: units!, priceEntryMode: priceMode as 1 | 2,
      vatRate: vatRate!, enteredUnitPrice: price!, lineDiscountType: discount.type, lineDiscountValue: discount.value, lineDiscountTaxBasis: discount.taxBasis,
      lineDiscountUnitBasis: discount.unitBasis, isInvoiceDiscountEligible: line.isInvoiceDiscountEligible,
    }];
  });
}

function parseDiscount(errors: Record<string, string[]>, prefix: string, typeText: string, valueText: string, taxText: string, unitText: string, invoice: boolean): { type?: 1 | 2 | 3 | 4; value?: number | null; taxBasis?: 1 | 2; unitBasis?: 1 | 2 | 3 } {
  if (!typeText && !valueText && !taxText && !unitText) return {};
  const type = integer(typeText);
  const value = decimal(valueText);
  const tax = integer(taxText);
  const unit = integer(unitText);
  const allowed = invoice ? type === 1 || type === 4 : type === 1 || type === 2 || type === 3;
  if (!allowed) errors[`${prefix}Type`] = ["Geçerli bir indirim türü seçin."];
  if (value === null || value < 0 || (type === 1 && value > 100)) errors[`${prefix}Value`] = ["İndirim değeri geçerli değil."];
  if (tax !== 1 && tax !== 2) errors[`${prefix}TaxBasis`] = ["İndirim vergi bazını seçin."];
  if (!invoice && type === 2 && unit !== 1 && unit !== 2 && unit !== 3) errors[`${prefix}UnitBasis`] = ["Sabit birim indirimi için birim bazını seçin."];
  if (!invoice && type !== 2 && unitText) errors[`${prefix}UnitBasis`] = ["Birim bazı yalnız sabit birim indiriminde kullanılabilir."];
  return { type: type as 1 | 2 | 3 | 4, value, taxBasis: tax as 1 | 2, unitBasis: type === 2 ? unit as 1 | 2 | 3 : undefined };
}

function parseInvoiceHeader(errors: Record<string, string[]>, number: string, date: string, dueDate: string, description: string, prefix: string): SalesInvoiceHeaderInput {
  const key = (name: string) => prefix ? `${prefix}${name[0]?.toUpperCase()}${name.slice(1)}` : name;
  length(errors, key("invoiceNumber"), number, 1, 100, "Fatura numarası");
  validDate(errors, key("invoiceDate"), date, "Fatura tarihi");
  optionalDate(errors, key("dueDate"), dueDate, "Fatura vade tarihi");
  length(errors, key("description"), description, 0, 500, "Fatura açıklaması");
  return { invoiceNumber: number, invoiceDate: toIsoDate(date), dueDate: dueDate ? toIsoDate(dueDate) : null, description: description || null };
}

function normalizeLine(value: unknown, index: number): SalesLineDraft {
  const record = object(value);
  return {
    key: string(record.key) || `line-${index + 1}`, lineNumber: string(record.lineNumber), productVariantId: string(record.productVariantId), quantity: string(record.quantity), unitOfMeasure: string(record.unitOfMeasure),
    unitsPerSaleUnit: string(record.unitsPerSaleUnit), priceEntryMode: string(record.priceEntryMode), vatRate: string(record.vatRate), enteredUnitPrice: string(record.enteredUnitPrice),
    lineDiscountType: string(record.lineDiscountType), lineDiscountValue: string(record.lineDiscountValue), lineDiscountTaxBasis: string(record.lineDiscountTaxBasis), lineDiscountUnitBasis: string(record.lineDiscountUnitBasis),
    isInvoiceDiscountEligible: record.isInvoiceDiscountEligible !== false,
  };
}

function text(data: FormData, name: string): string { const value = data.get(name); return typeof value === "string" ? value.trim() : ""; }
function object(value: unknown): Record<string, unknown> { return value !== null && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {}; }
function string(value: unknown): string { return typeof value === "string" || typeof value === "number" ? String(value).trim() : ""; }
function decimal(value: string): number | null { const number = Number(value.replace(",", ".")); return value !== "" && Number.isFinite(number) ? number : null; }
function integer(value: string): number | null { const number = Number(value); return Number.isInteger(number) ? number : null; }
function parseJson<T>(value: string, fallback: T): T { try { return JSON.parse(value) as T; } catch { return fallback; } }
function toIsoDate(value: string): string { return `${value}T00:00:00.000Z`; }
function length(errors: Record<string, string[]>, key: string, value: string, min: number, max: number, label: string): void { if (value.length < min || value.length > max) errors[key] = [`${label} ${min}–${max} karakter olmalıdır.`]; }
function validDate(errors: Record<string, string[]>, key: string, value: string, label: string): void { if (!DATE.test(value) || Number.isNaN(new Date(`${value}T00:00:00Z`).getTime())) errors[key] = [`${label} geçerli bir tarih olmalıdır.`]; }
function optionalDate(errors: Record<string, string[]>, key: string, value: string, label: string): void { if (value) validDate(errors, key, value, label); }
