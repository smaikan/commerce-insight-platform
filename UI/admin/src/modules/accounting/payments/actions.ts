"use server";

import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { cancelPayment, createPayment } from "./api";
import { parsePaymentForm } from "./form-data";
import type { Payment, PaymentFormState, PaymentInput } from "./types";

export async function createPaymentAction(_previous: PaymentFormState, formData: FormData): Promise<PaymentFormState> {
  const parsed = parsePaymentForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    const payment = await createPayment(parsed.input, parsed.draft.idempotencyKey, session);
    if (!matchesPaymentIntent(payment, parsed.input)) {
      return { status: "error", message: "İşlem anahtarı daha önce farklı bir ödeme için kullanılmış. Kayıt açılmadı; yeni bir anahtarla yeniden deneyin.", draft: parsed.draft };
    }
    refreshPayment(payment.id, payment.currentAccountId);
    return { status: "success", message: parsed.input.type === 1 ? "Müşteri tahsilatı kaydedildi." : parsed.input.allocations.length ? "Tedarikçi ödemesi kaydedildi." : "Dağıtımsız tedarikçi avansı kaydedildi.", redirectHref: `/accounting/payments/${encodeURIComponent(payment.id)}?created=1` };
  } catch (error) { return paymentError(error, parsed.draft, "Ödeme kaydedilemedi.", "Açık kalem bakiyesi başka bir işlemle değişti. Güncel kalemler yeniden okunacak; otomatik retry yapılmadı.", true); }
}

export async function cancelPaymentAction(id: string, currentAccountId: string, _previous: PaymentFormState, formData: FormData): Promise<PaymentFormState> {
  const reason = typeof formData.get("reason") === "string" ? String(formData.get("reason")).trim() : "";
  if (!reason || reason.length > 500) return { status: "error", message: "İptal gerekçesi 1–500 karakter olmalıdır." };
  try {
    const session = await requireAdminActionSession(); await cancelPayment(id, reason, session); refreshPayment(id, currentAccountId);
    return { status: "success", message: "Ödeme iptal edildi; kasa/banka, cari ve açık kalem bakiyeleri API'den yeniden okunuyor.", refresh: true };
  } catch (error) { return paymentError(error, undefined, "Ödeme iptal edilemedi.", "Ödeme durumu başka bir işlemle değişti. Güncel detay yeniden okunacak; otomatik retry yapılmadı.", true); }
}
function refreshPayment(id: string, currentAccountId: string): void { revalidatePath("/accounting"); revalidatePath("/accounting/payments"); revalidatePath(`/accounting/payments/${encodeURIComponent(id)}`); revalidatePath(`/accounting/current-accounts/${encodeURIComponent(currentAccountId)}`); revalidatePath("/accounting/treasury"); }
function matchesPaymentIntent(payment: Payment, input: PaymentInput): boolean {
  if (payment.status !== 1 || payment.currentAccountId !== input.currentAccountId || payment.type !== input.type || payment.direction !== (input.type === 1 ? 1 : 2) || Math.round(payment.amount * 100) !== Math.round(input.amount * 100) || Date.parse(payment.paymentDate) !== Date.parse(input.paymentDate) || (payment.cashAccountId ?? null) !== (input.cashAccountId ?? null) || (payment.bankAccountId ?? null) !== (input.bankAccountId ?? null) || (payment.referenceNumber ?? null) !== (input.referenceNumber ?? null) || (payment.description ?? null) !== (input.description ?? null) || payment.allocations.length !== input.allocations.length) return false;
  const returned = new Map(payment.allocations.map((item) => [item.currentAccountTransactionId, Math.round(item.allocatedAmount * 100)]));
  return input.allocations.every((item) => returned.get(item.currentAccountTransactionId) === Math.round(item.amount * 100));
}
function paymentError(error: unknown, draft: PaymentFormState["draft"], fallback: string, conflict: string, refresh: boolean): PaymentFormState {
  if (!(error instanceof ApiError)) return { status: "error", message: fallback, draft };
  const p = error.problem; const message = p.status === 401 ? "Oturumunuz sona erdi. Yeniden giriş yapın." : p.status === 403 ? "Bu işlem için yönetici yetkiniz yok." : p.status === 404 ? "Cari, açık kalem veya finans hesabı artık bulunamıyor." : p.status === 409 ? conflict : p.status === 429 ? p.retryAfter ? `İstek sınırına ulaşıldı. ${p.retryAfter} sonra aynı işlemle tekrar deneyin.` : "İstek sınırına ulaşıldı." : p.detail || p.title || fallback;
  return { status: "error", message, code: p.code, traceId: p.traceId, retryAfter: p.retryAfter, fieldErrors: p.errors, draft, refresh: refresh && p.status === 409 };
}
