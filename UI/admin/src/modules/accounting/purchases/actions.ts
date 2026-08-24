"use server";

import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  addPurchaseInvoiceExpense,
  cancelPurchaseInvoice,
  createExpenseCategory,
  createGeneralExpense,
  createPurchaseInvoice,
  postPurchaseInvoice,
  setPurchaseInvoiceAllocations,
} from "./api";
import {
  parseAllocationForm,
  parseExpenseCategoryForm,
  parseGeneralExpenseForm,
  parsePurchaseInvoiceExpenseForm,
  parsePurchaseInvoiceForm,
} from "./form-data";
import type {
  AccountingFormState,
  ExpenseCategoryDraft,
  GeneralExpenseDraft,
  PurchaseInvoiceAllocationDraft,
  PurchaseInvoiceExpenseDraft,
  PurchaseInvoiceFormDraft,
} from "./types";

// Burada yeni alış faturasını tek belge intent'i olarak kaydediyorum. Mevcut taslak güncellemesi, API kayıpsız round-trip sağlamadan bu action'a bağlanmaz.
export async function savePurchaseInvoiceAction(_previous: AccountingFormState<PurchaseInvoiceFormDraft>, formData: FormData): Promise<AccountingFormState<PurchaseInvoiceFormDraft>> {
  const parsed = parsePurchaseInvoiceForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    const invoice = await createPurchaseInvoice(parsed.header, parsed.lines, session);
    refreshPurchaseInvoice(invoice.id);
    return { status: "success", message: "Alış faturası taslağı oluşturuldu.", redirectHref: `/accounting/purchase-invoices/${encodeURIComponent(invoice.id)}?created=1` };
  } catch (error) {
    return purchaseError(error, parsed.draft, "Alış faturası kaydedilemedi.", "Fatura numarası veya taslak satırları güncel kayıtlarla çakışıyor.", true);
  }
}

// Burada satırın mevcut Purchase hareketi tahsislerini topluca değiştirip güncel belgeyi yeniden okutuyorum.
export async function savePurchaseInvoiceAllocationsAction(invoiceId: string, lineId: string, _previous: AccountingFormState<PurchaseInvoiceAllocationDraft>, formData: FormData): Promise<AccountingFormState<PurchaseInvoiceAllocationDraft>> {
  const parsed = parseAllocationForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    await setPurchaseInvoiceAllocations(invoiceId, lineId, parsed.allocations, session);
    refreshPurchaseInvoice(invoiceId);
    return { status: "success", message: "Stok hareketi tahsisleri güncellendi.", refresh: true };
  } catch (error) {
    return purchaseError(error, parsed.draft, "Tahsisler kaydedilemedi.", "Stok hareketinin kullanılabilir miktarı değişti veya satır artık taslak değil. Güncel veri yeniden okunacak; işlem otomatik tekrarlanmadı.", true);
  }
}

// Burada tam tahsisli taslağı post edip supplier debt ve FIFO etkilerinin authoritative detaydan yeniden okunmasını istiyorum.
export async function postPurchaseInvoiceAction(invoiceId: string, _previous: AccountingFormState): Promise<AccountingFormState> {
  void _previous;
  try {
    const session = await requireAdminActionSession();
    await postPurchaseInvoice(invoiceId, session);
    refreshPurchaseInvoice(invoiceId);
    return { status: "success", message: "Alış faturası post edildi. Tedarikçi borcu ve maliyet etkileri güncel API verisinden okundu.", refresh: true };
  } catch (error) {
    return purchaseError(error, undefined, "Alış faturası post edilemedi.", "Satırlardan biri tam tahsis değil veya stok hareketi uygunluğu değişti. Güncel detay yeniden okunacak; post otomatik tekrarlanmadı.", true);
  }
}

// Burada yalnız Posted faturayı gerekçeyle iptal ediyor ve reversal geçmişini silmeden yeniden okutuyorum.
export async function cancelPurchaseInvoiceAction(invoiceId: string, _previous: AccountingFormState, formData: FormData): Promise<AccountingFormState> {
  const reason = typeof formData.get("reason") === "string" ? String(formData.get("reason")).trim() : "";
  if (!reason || reason.length > 500) return { status: "error", message: "İptal gerekçesi 1–500 karakter olmalıdır." };
  try {
    const session = await requireAdminActionSession();
    await cancelPurchaseInvoice(invoiceId, reason, session);
    refreshPurchaseInvoice(invoiceId);
    return { status: "success", message: "Alış faturası iptal edildi; ters kayıt ve tarihsel belge yeniden okundu.", refresh: true };
  } catch (error) {
    return purchaseError(error, undefined, "Alış faturası iptal edilemedi.", "Geçerli tedarikçi ödeme tahsisi veya tüketilmiş FIFO katmanı iptali engelliyor olabilir. Güncel detay yeniden okunacak; otomatik ya da zorunlu retry yapılmadı.", true);
  }
}

// Burada taslak faturaya gider ekleyip final maliyetleri yalnız yeniden okunan fatura DTO'sundan gösteriyorum.
export async function addPurchaseInvoiceExpenseAction(invoiceId: string, lineIds: string[], _previous: AccountingFormState<PurchaseInvoiceExpenseDraft>, formData: FormData): Promise<AccountingFormState<PurchaseInvoiceExpenseDraft>> {
  const parsed = parsePurchaseInvoiceExpenseForm(formData, lineIds);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    await addPurchaseInvoiceExpense(invoiceId, parsed.input, session);
    refreshPurchaseInvoice(invoiceId);
    return { status: "success", message: "Fatura gideri dağıtıldı; final maliyetler yeniden okundu.", refresh: true };
  } catch (error) {
    return purchaseError(error, parsed.draft, "Fatura gideri eklenemedi.", "Fatura, kategori veya manuel dağıtım güncel kayıtlarla çakışıyor. Taslak korundu.", true);
  }
}

export async function createExpenseCategoryAction(_previous: AccountingFormState<ExpenseCategoryDraft>, formData: FormData): Promise<AccountingFormState<ExpenseCategoryDraft>> {
  const parsed = parseExpenseCategoryForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    await createExpenseCategory(parsed.input, session);
    revalidatePath("/accounting/expenses");
    return { status: "success", message: "Gider kategorisi oluşturuldu.", refresh: true };
  } catch (error) {
    return purchaseError(error, parsed.draft, "Gider kategorisi oluşturulamadı.", "Kategori kodu zaten kullanılıyor. Farklı bir kod girin.");
  }
}

export async function createGeneralExpenseAction(_previous: AccountingFormState<GeneralExpenseDraft>, formData: FormData): Promise<AccountingFormState<GeneralExpenseDraft>> {
  const parsed = parseGeneralExpenseForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    await createGeneralExpense(parsed.input, session);
    revalidatePath("/accounting/expenses");
    return { status: "success", message: "Genel gider kaydedildi. Bu kayıt stok maliyetini değiştirmez.", refresh: true };
  } catch (error) {
    return purchaseError(error, parsed.draft, "Genel gider kaydedilemedi.", "Kategori veya gider bilgileri güncel kayıtlarla çakışıyor.");
  }
}

function refreshPurchaseInvoice(id: string): void {
  revalidatePath("/accounting");
  revalidatePath("/accounting/purchase-invoices");
  revalidatePath(`/accounting/purchase-invoices/${encodeURIComponent(id)}`);
  revalidatePath("/accounting/expenses");
}

function purchaseError<TDraft>(error: unknown, draft: TDraft, fallback: string, conflict: string, refreshOnConflict = false): AccountingFormState<TDraft> {
  if (!(error instanceof ApiError)) return { status: "error", message: fallback, draft };
  const problem = error.problem;
  const message = problem.status === 401
    ? "Oturumunuz sona erdi. Yeniden giriş yapın."
    : problem.status === 403
      ? "Bu işlem için yönetici yetkiniz yok."
      : problem.status === 404
        ? "Kayıt artık bulunamıyor. Listeyi veya detayı yenileyin."
        : problem.status === 409
          ? conflict
          : problem.status === 429
            ? problem.retryAfter ? `İstek sınırına ulaşıldı. ${problem.retryAfter} sonra tekrar deneyin.` : "İstek sınırına ulaşıldı. Bir süre sonra tekrar deneyin."
            : problem.detail || problem.title || fallback;
  return { status: "error", message, code: problem.code, traceId: problem.traceId, retryAfter: problem.retryAfter, fieldErrors: problem.errors, draft, refresh: refreshOnConflict && problem.status === 409 };
}
