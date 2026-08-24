"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  cancelSalesOrder,
  createDirectSalesInvoice,
  createSalesInvoiceFromOrder,
  createSalesOrder,
  postSalesInvoice,
  postSalesOrder,
  updateSalesInvoice,
  updateSalesOrder,
} from "./api";
import { parseInvoiceFromOrderForm, parseSalesInvoiceEditForm, parseSalesOrderForm } from "./form-data";
import type { InvoiceFromOrderDraft, SalesFormState, SalesInvoiceEditDraft, SalesOrderFormDraft } from "./types";

export async function saveSalesOrderAction(id: string | undefined, _previous: SalesFormState<SalesOrderFormDraft>, formData: FormData): Promise<SalesFormState<SalesOrderFormDraft>> {
  const parsed = parseSalesOrderForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    const order = id
      ? await updateSalesOrder(id, parsed.header, parsed.lines, session)
      : await createSalesOrder(parsed.header, parsed.lines, parsed.draft.createInvoice, parsed.invoice, parsed.draft.idempotencyKey, session);
    refreshSales(order.id, order.salesInvoiceId);
    return { status: "success", message: id ? "Muhasebe satışı güncellendi." : "Muhasebe satışı taslak olarak oluşturuldu.", redirectHref: `/accounting/sales-orders/${encodeURIComponent(order.id)}?${id ? "updated" : "created"}=1` };
  } catch (error) {
    return salesError(error, parsed.draft, "Muhasebe satışı kaydedilemedi.", "Satış numarası, fatura numarası veya işlem anahtarı güncel bir kayıtla çakışıyor.", true);
  }
}

export async function createDirectSalesInvoiceAction(_previous: SalesFormState<SalesOrderFormDraft>, formData: FormData): Promise<SalesFormState<SalesOrderFormDraft>> {
  const parsed = parseSalesOrderForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    if (!parsed.invoice) return { status: "error", message: "Doğrudan fatura için fatura başlığı zorunludur.", draft: parsed.draft };
    const invoice = await createDirectSalesInvoice(parsed.header, parsed.invoice, parsed.lines, parsed.draft.idempotencyKey, session);
    refreshSales(invoice.accountingSalesOrderId, invoice.id);
    return { status: "success", message: "Satış faturası ve bağlı muhasebe satışı taslak olarak oluşturuldu.", redirectHref: `/accounting/sales-invoices/${encodeURIComponent(invoice.id)}?created=1` };
  } catch (error) {
    return salesError(error, parsed.draft, "Satış faturası kaydedilemedi.", "Satış/fatura numarası veya işlem anahtarı güncel bir kayıtla çakışıyor.", true);
  }
}

export async function updateSalesInvoiceAction(invoiceId: string, _previous: SalesFormState<SalesInvoiceEditDraft>, formData: FormData): Promise<SalesFormState<SalesInvoiceEditDraft>> {
  const parsed = parseSalesInvoiceEditForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    const invoice = await updateSalesInvoice(invoiceId, parsed.header, parsed.lines, session);
    refreshSales(invoice.accountingSalesOrderId, invoice.id);
    return { status: "success", message: "Satış faturası ve bağlı taslak satış satırları güncellendi.", redirectHref: `/accounting/sales-invoices/${encodeURIComponent(invoice.id)}?updated=1` };
  } catch (error) {
    return salesError(error, parsed.draft, "Satış faturası kaydedilemedi.", "Satış/fatura numarası güncel bir kayıtla çakışıyor veya belge artık taslak değil.", true);
  }
}

export async function createInvoiceFromOrderAction(orderId: string, _previous: SalesFormState<InvoiceFromOrderDraft>, formData: FormData): Promise<SalesFormState<InvoiceFromOrderDraft>> {
  const parsed = parseInvoiceFromOrderForm(formData);
  if (!parsed.ok) return parsed.state;
  let createdInvoiceId: string;
  try {
    const session = await requireAdminActionSession();
    const invoice = await createSalesInvoiceFromOrder(orderId, parsed.header, session);
    createdInvoiceId = invoice.id;
  } catch (error) {
    return salesError(error, parsed.draft, "Satış faturası oluşturulamadı.", "Bu satışta farklı başlıklı bir fatura zaten bulunuyor veya fatura numarası çakışıyor.", true);
  }
  // Action sonrası mevcut order tree'si fatura formunu kaldırdığı için yönlendirmeyi client effect yarışına bırakmıyorum.
  refreshSales(orderId, createdInvoiceId);
  redirect(`/accounting/sales-invoices/${encodeURIComponent(createdInvoiceId)}?createdFromOrder=1`);
}

export async function postSalesOrderAction(orderId: string, _previous: SalesFormState): Promise<SalesFormState> {
  void _previous;
  try {
    const session = await requireAdminActionSession();
    const order = await postSalesOrder(orderId, session);
    refreshSales(order.id, order.salesInvoiceId);
    return { status: "success", message: "Satış post edildi; stok çıkışı, FIFO maliyeti ve müşteri alacağı güncel detaydan yeniden okunuyor.", refresh: true };
  } catch (error) {
    return salesError(error, undefined, "Satış post edilemedi.", "Fiziksel stok, StockMovement bakiyesi, aktif varyant veya FIFO katmanları satış miktarını karşılamıyor. Güncel durum yeniden okunacak; otomatik retry yapılmadı.", true);
  }
}

export async function postSalesInvoiceAction(invoiceId: string, orderId: string, _previous: SalesFormState): Promise<SalesFormState> {
  void _previous;
  try {
    const session = await requireAdminActionSession();
    const invoice = await postSalesInvoice(invoiceId, session);
    refreshSales(orderId, invoice.id);
    return { status: "success", message: "Bağlı muhasebe satışı post edildi; fatura ikinci bir stok veya alacak etkisi oluşturmadı.", refresh: true };
  } catch (error) {
    return salesError(error, undefined, "Satış faturası post edilemedi.", "Bağlı satışın stok veya FIFO koşulları değişti. Güncel satış ve fatura yeniden okunacak; otomatik retry yapılmadı.", true);
  }
}

export async function cancelSalesOrderAction(orderId: string, invoiceId: string | undefined, _previous: SalesFormState, formData: FormData): Promise<SalesFormState> {
  const reason = typeof formData.get("reason") === "string" ? String(formData.get("reason")).trim() : "";
  if (!reason || reason.length > 500) return { status: "error", message: "İptal gerekçesi 1–500 karakter olmalıdır." };
  try {
    const session = await requireAdminActionSession();
    await cancelSalesOrder(orderId, reason, session);
    refreshSales(orderId, invoiceId);
    return { status: "success", message: "Satış iptal edildi; stok, FIFO, müşteri alacağı ve bağlı fatura reversal durumu yeniden okunuyor.", refresh: true };
  } catch (error) {
    return salesError(error, undefined, "Satış iptal edilemedi.", "Satışa tahsis edilmiş geçerli bir müşteri tahsilatı bulunuyor olabilir. Önce tahsilatı tersleyin; otomatik retry yapılmadı.", true);
  }
}

function refreshSales(orderId: string, invoiceId?: string | null): void {
  refreshSalesLists();
  revalidatePath(`/accounting/sales-orders/${encodeURIComponent(orderId)}`);
  if (invoiceId) revalidatePath(`/accounting/sales-invoices/${encodeURIComponent(invoiceId)}`);
}

function refreshSalesLists(): void { revalidatePath("/accounting"); revalidatePath("/accounting/sales-orders"); revalidatePath("/accounting/sales-invoices"); }

function salesError<TDraft>(error: unknown, draft: TDraft, fallback: string, conflict: string, refreshOnConflict = false): SalesFormState<TDraft> {
  if (!(error instanceof ApiError)) return { status: "error", message: fallback, draft };
  const p = error.problem;
  const message = p.status === 401 ? "Oturumunuz sona erdi. Yeniden giriş yapın." : p.status === 403 ? "Bu işlem için yönetici yetkiniz yok." : p.status === 404 ? "Satış belgesi artık bulunamıyor. Listeyi yenileyin." : p.status === 409 ? conflict : p.status === 429 ? p.retryAfter ? `İstek sınırına ulaşıldı. ${p.retryAfter} sonra aynı işlemle tekrar deneyin.` : "İstek sınırına ulaşıldı. Bir süre sonra aynı işlemle tekrar deneyin." : p.detail || p.title || fallback;
  return { status: "error", message, code: p.code, traceId: p.traceId, retryAfter: p.retryAfter, fieldErrors: p.errors, draft, refresh: refreshOnConflict && p.status === 409 };
}
