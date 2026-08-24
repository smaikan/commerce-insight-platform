"use server";

import { adminMutationError } from "@/lib/admin/mutation-error";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  advanceReturnRequest,
  decideReturnRequest,
  getReturnRequest,
  updateOrderStatus,
} from "@/modules/orders/api";
import { isManagedOrderStatus } from "@/modules/orders/lifecycle";
import type { ReturnMutationResult } from "@/modules/orders/return-action-state";
import { availableReturnActions, type ReturnActionIntent } from "@/modules/orders/return-lifecycle";
import type { OrderStatusMutationResult } from "@/modules/orders/status-action-state";

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

// Burada sipariş durum formunu allowlist ile doğrulayıp yalnız genel sipariş yaşam döngüsü endpoint'ine gönderiyorum.
export async function updateOrderStatusAction(
  _previousState: AdminMutationResult | null,
  formData: FormData,
): Promise<OrderStatusMutationResult> {
  const orderId = readString(formData, "orderId");
  const status = Number(formData.get("status"));
  if (!orderId || !uuidPattern.test(orderId) || !isManagedOrderStatus(status)) {
    return { status: "error", message: "Sipariş durumu isteği geçersiz." };
  }

  const shippingCarrier = readOptionalString(formData, "shippingCarrier");
  const trackingNumber = readOptionalString(formData, "trackingNumber");
  const trackingUrl = readOptionalString(formData, "trackingUrl");
  if (status === 4 && (!shippingCarrier || !trackingNumber)) {
    return { status: "error", message: "Kargoya verilen siparişte taşıyıcı ve takip numarası zorunludur." };
  }

  try {
    const updatedOrder = await updateOrderStatus(orderId, {
      status,
      shippingCarrier: status === 4 ? shippingCarrier : null,
      trackingNumber: status === 4 ? trackingNumber : null,
      trackingUrl: status === 4 ? trackingUrl : null,
    }, await requireAdminActionSession());
    return {
      status: "success",
      message: "Sipariş durumu güncellendi.",
      orderStatus: updatedOrder.status,
    };
  } catch (error) {
    return mutationError(
      error,
      "Sipariş durumu güncellenemedi.",
      "Sipariş durumu bu geçiş için artık uygun değil. Güncel siparişi kontrol edip tekrar deneyin.",
    );
  }
}

// Burada iade formundaki talep düzeyi kararı doğrulayıp yalnız mevcut dört yaşam döngüsü işleminden birini çalıştırıyorum.
export async function manageReturnRequestAction(
  _previousState: AdminMutationResult | null,
  formData: FormData,
): Promise<ReturnMutationResult> {
  const orderId = readString(formData, "orderId");
  const returnRequestId = readString(formData, "returnRequestId");
  const intent = readString(formData, "intent");
  if (!orderId || !uuidPattern.test(orderId) || !returnRequestId || !uuidPattern.test(returnRequestId)) {
    return { status: "error", message: "İade talebi kimliği geçersiz." };
  }
  if (!isReturnActionIntent(intent)) {
    return { status: "error", message: "İade işlemi geçersiz." };
  }

  const decisionNote = readOptionalString(formData, "decisionNote");
  if (decisionNote && decisionNote.length > 1_000) {
    return { status: "error", message: "Karar notu en fazla 1000 karakter olabilir." };
  }

  try {
    const session = await requireAdminActionSession();
    const currentReturn = await getReturnRequest(returnRequestId, session);
    if (currentReturn.orderId !== orderId) {
      return { status: "error", message: "İade talebi bu siparişe ait değil." };
    }
    if (!availableReturnActions(currentReturn).includes(intent)) {
      return {
        status: "error",
        message: "Bu işlem iade talebinin güncel durumu için geçerli değil. Güncel talebi inceleyip işlemi yeniden seçin.",
        refresh: true,
      };
    }
    const updatedReturn = intent === "approve" || intent === "reject"
      ? await decideReturnRequest(returnRequestId, intent, decisionNote, session)
      : await advanceReturnRequest(returnRequestId, intent, session);
    return {
      status: "success",
      message: returnActionSuccessMessage(intent, currentReturn.type, currentReturn.status),
      returnRequest: updatedReturn,
    };
  } catch (error) {
    return returnMutationError(error);
  }
}

// Burada çakışan yaşam döngüsü mutasyonunda güncel sipariş ve iade verisinin yeniden okunmasını işaretliyorum.
function mutationError(error: unknown, fallback: string, conflictMessage: string): AdminMutationResult {
  const result = adminMutationError(error, fallback, conflictMessage);
  if (error instanceof ApiError && error.problem.status === 409) {
    return { ...result, refresh: true };
  }
  return result;
}

// Burada yaşam döngüsü işleminden sonra kalıcı bölgede gösterilecek kısa başarı mesajını seçiyorum.
function returnActionSuccessMessage(intent: ReturnActionIntent, type: 0 | 1, previousStatus: number): string {
  if (intent === "approve") {
    return type === 0
      ? "İade talebi onaylandı; sipariş Ücret İade Edildi durumuna güncellendi."
      : "Değişim talebi onaylandı; iade ve değişim stok hareketleri tamamlandı.";
  }
  if (intent === "reject") return "İade talebi reddedildi.";
  if (intent === "receive") {
    return previousStatus === 0
      ? "İade ürünleri teslim alındı; talep karar bekliyor."
      : "Eski akıştaki onaylı iade ürünleri teslim alındı.";
  }
  return "Eski iade süreci tamamlandı.";
}

// Burada yeni typed geçiş hatasını gerçek concurrency ve stok çakışmalarından ayırıyorum.
function returnMutationError(error: unknown): AdminMutationResult {
  if (error instanceof ApiError && error.problem.status === 409) {
    const message = error.problem.code === "return_status_transition_invalid"
      ? "İade talebinin durumu değişti. Güncel talebi inceleyip işlemi yeniden seçin."
      : error.problem.code === "concurrency_conflict"
        ? "İade talebi başka bir işlemle eşzamanlı değiştirildi. Güncel talebi inceleyip işlemi yeniden seçin."
        : error.problem.detail || "İade işlemi stok veya varyant koşulları nedeniyle tamamlanamadı.";
    return { status: "error", message, traceId: error.problem.traceId, refresh: true };
  }
  return adminMutationError(
    error,
    "İade talebi güncellenemedi.",
    "İade talebi güncellenemedi. Güncel talebi kontrol edip tekrar deneyin.",
  );
}

// Burada form intent'ini dört belgelenmiş return operasyonuyla sınırlandırıyorum.
function isReturnActionIntent(intent: string | undefined): intent is ReturnActionIntent {
  return intent === "approve" || intent === "reject" || intent === "receive" || intent === "complete";
}

// Burada FormData içindeki zorunlu metni tek değer ve boşluk normalizasyonuyla okuyorum.
function readString(formData: FormData, name: string): string | undefined {
  const value = formData.get(name);
  return typeof value === "string" && value.trim() ? value.trim() : undefined;
}

// Burada boş bırakılabilen metinleri backend'e null olarak taşıyacak biçimde normalize ediyorum.
function readOptionalString(formData: FormData, name: string): string | null {
  return readString(formData, name) ?? null;
}
