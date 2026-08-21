"use server";

import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  addContactMessageNote,
  assignContactMessage,
  changeContactMessageStatus,
  getContactMessage,
  replyToContactMessage,
} from "@/modules/contact-messages/api";
import { contactMessageMutationError, contactMessageSnapshot } from "@/modules/contact-messages/mutation";
import { contactMessageStatusTransitions } from "@/modules/contact-messages/presentation";
import type {
  ContactMessageActionResult,
  ContactMessageStatus,
} from "@/modules/contact-messages/types";

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const publicAdminIdPattern = /^U[0-9A-Z]{5,7}$/;

type ConcurrencyInput = { messageId: string; expectedConcurrencyToken: string };

// Burada status seçimini yayımlanmış geçiş matrisi ve stale-token korumasıyla uygularım.
export async function changeContactMessageStatusAction(input: ConcurrencyInput & { currentStatus: ContactMessageStatus; status: ContactMessageStatus }): Promise<ContactMessageActionResult> {
  if (!validConcurrencyInput(input) || !contactMessageStatusTransitions(input.currentStatus).includes(input.status)) {
    return { status: "error", message: "Durum geçişi geçersiz." };
  }
  try {
    const detail = await changeContactMessageStatus(input.messageId, {
      status: input.status,
      expectedConcurrencyToken: input.expectedConcurrencyToken,
    }, await requireAdminActionSession());
    revalidateContactMessagePaths(input.messageId);
    return { status: "success", message: "Mesaj durumu güncellendi.", snapshot: contactMessageSnapshot(detail) };
  } catch (error) {
    return conflictAwareResult(input.messageId, error, "Mesaj durumu güncellenemedi.");
  }
}

// Burada atama seçimini aktif Admin public ID allowlist'i ve concurrency token ile uygularım.
export async function assignContactMessageAction(input: ConcurrencyInput & { assignedAdminUserId: string | null }): Promise<ContactMessageActionResult> {
  if (!validConcurrencyInput(input) || (input.assignedAdminUserId !== null && !publicAdminIdPattern.test(input.assignedAdminUserId))) {
    return { status: "error", message: "Yönetici ataması geçersiz." };
  }
  try {
    const detail = await assignContactMessage(input.messageId, {
      assignedAdminUserId: input.assignedAdminUserId,
      expectedConcurrencyToken: input.expectedConcurrencyToken,
    }, await requireAdminActionSession());
    revalidateContactMessagePaths(input.messageId);
    return { status: "success", message: input.assignedAdminUserId ? "Mesaj yöneticiye atandı." : "Mesaj ataması kaldırıldı.", snapshot: contactMessageSnapshot(detail) };
  } catch (error) {
    return conflictAwareResult(input.messageId, error, "Mesaj ataması güncellenemedi.");
  }
}

// Burada append-only dahili notu boşluk ve 2000 karakter sınırından sonra kaydederim.
export async function addContactMessageNoteAction(input: ConcurrencyInput & { note: string }): Promise<ContactMessageActionResult> {
  const note = input.note.trim();
  if (!validConcurrencyInput(input) || !note || note.length > 2_000) {
    return { status: "error", message: "Dahili not 1–2000 karakter olmalıdır.", fieldErrors: { note: ["Dahili not 1–2000 karakter olmalıdır."] } };
  }
  try {
    const detail = await addContactMessageNote(input.messageId, {
      note,
      expectedConcurrencyToken: input.expectedConcurrencyToken,
    }, await requireAdminActionSession());
    revalidateContactMessagePaths(input.messageId);
    return { status: "success", message: "Dahili not activity akışına eklendi.", snapshot: contactMessageSnapshot(detail) };
  } catch (error) {
    return conflictAwareResult(input.messageId, error, "Dahili not eklenemedi.");
  }
}

// Burada müşteri yanıtını 202 kuyruğa alma semantiği ve intent boyunca sabit kalan key ile gönderirim.
export async function replyContactMessageAction(input: { messageId: string; body: string; idempotencyKey: string }): Promise<ContactMessageActionResult> {
  const body = input.body.trim();
  if (!uuidPattern.test(input.messageId) || !body || body.length > 5_000 || !input.idempotencyKey || input.idempotencyKey.length > 200) {
    return { status: "error", message: "Müşteri yanıtı 1–5000 karakter olmalıdır.", fieldErrors: { body: ["Müşteri yanıtı 1–5000 karakter olmalıdır."] } };
  }
  try {
    const detail = await replyToContactMessage(input.messageId, { body }, input.idempotencyKey, await requireAdminActionSession());
    revalidateContactMessagePaths(input.messageId);
    return { status: "success", message: "Yanıt gönderim sırasına alındı.", snapshot: contactMessageSnapshot(detail) };
  } catch (error) {
    return contactMessageMutationError(error, "Yanıt gönderim sırasına alınamadı.");
  }
}

// Burada yalnız concurrency_conflict kodunda authoritative detail'i yeniden okuyup dar snapshot döndürüyorum.
async function conflictAwareResult(messageId: string, error: unknown, fallback: string): Promise<ContactMessageActionResult> {
  if (error instanceof ApiError && error.problem.status === 409 && error.problem.code === "concurrency_conflict") {
    try {
      const detail = await getContactMessage(messageId, await requireAdminActionSession());
      return {
        status: "conflict",
        code: "concurrency_conflict",
        message: "Kayıt başka bir yönetici tarafından değiştirildi. Güncel durumu inceleyip kararınızı yeniden verin.",
        snapshot: contactMessageSnapshot(detail),
        traceId: error.problem.traceId,
      };
    } catch {
      return { status: "conflict", code: "concurrency_conflict", message: "Kayıt değişti ancak güncel hali alınamadı. Sayfayı yenileyin.", traceId: error.problem.traceId };
    }
  }
  return contactMessageMutationError(error, fallback);
}

// Burada path ve token değerlerini backend çağrısından önce UUID biçiminde sınırlarım.
function validConcurrencyInput(input: ConcurrencyInput): boolean {
  return uuidPattern.test(input.messageId) && uuidPattern.test(input.expectedConcurrencyToken);
}

// Burada mutation sonrası liste ve detail Server Component verisini authoritative olarak yeniden doğrularım.
function revalidateContactMessagePaths(messageId: string): void {
  revalidatePath("/contact-messages");
  revalidatePath(`/contact-messages/${encodeURIComponent(messageId)}`);
}
