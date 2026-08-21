"use server";

import { revalidatePath } from "next/cache";
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

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

// Burada sipariş durum formunu allowlist ile doğrulayıp yalnız genel sipariş yaşam döngüsü endpoint'ine gönderiyorum.
export async function updateOrderStatusAction(
  _previousState: AdminMutationResult | null,
  formData: FormData,
): Promise<AdminMutationResult> {
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
    await updateOrderStatus(orderId, {
      status,
      shippingCarrier: status === 4 ? shippingCarrier : null,
      trackingNumber: status === 4 ? trackingNumber : null,
      trackingUrl: status === 4 ? trackingUrl : null,
    }, await requireAdminActionSession());
    revalidateOrderPaths(orderId);
    return { status: "success", message: "Sipariş durumu güncellendi." };
  } catch (error) {
    return mutationError(
      orderId,
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
): Promise<AdminMutationResult> {
  const orderId = readString(formData, "orderId");
  const returnRequestId = readString(formData, "returnRequestId");
  const intent = readString(formData, "intent");
  if (!orderId || !uuidPattern.test(orderId) || !returnRequestId || !uuidPattern.test(returnRequestId)) {
    return { status: "error", message: "İade talebi kimliği geçersiz." };
  }
  if (intent !== "approve" && intent !== "reject" && intent !== "receive" && intent !== "complete") {
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
    if (intent === "approve" || intent === "reject") {
      await decideReturnRequest(returnRequestId, intent, decisionNote, session);
    } else {
      await advanceReturnRequest(returnRequestId, intent, session);
    }
    revalidateOrderPaths(orderId);
    return { status: "success", message: returnActionSuccessMessage(intent) };
  } catch (error) {
    return mutationError(
      orderId,
      error,
      "İade talebi güncellenemedi.",
      "İade talebi başka bir işlemle değişmiş olabilir. Güncel talebi kontrol edip tekrar deneyin.",
    );
  }
}

// Burada çakışan yaşam döngüsü mutasyonunda güncel sipariş ve iade verisinin yeniden okunmasını işaretliyorum.
function mutationError(orderId: string, error: unknown, fallback: string, conflictMessage: string): AdminMutationResult {
  const result = adminMutationError(error, fallback, conflictMessage);
  if (error instanceof ApiError && error.problem.status === 409) {
    revalidateOrderPaths(orderId);
    return { ...result, refresh: true };
  }
  return result;
}

// Burada değişen sipariş ve iade bilgisini liste, detay ve hızlı bakış sınırlarında yeniden doğrulatıyorum.
function revalidateOrderPaths(orderId: string): void {
  revalidatePath("/orders");
  revalidatePath(`/orders/${encodeURIComponent(orderId)}`);
}

// Burada yaşam döngüsü işleminden sonra kalıcı bölgede gösterilecek kısa başarı mesajını seçiyorum.
function returnActionSuccessMessage(intent: "approve" | "reject" | "receive" | "complete"): string {
  const messages = {
    approve: "İade talebi onaylandı.",
    reject: "İade talebi reddedildi.",
    receive: "İade ürünleri teslim alındı.",
    complete: "İade süreci tamamlandı.",
  };
  return messages[intent];
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
