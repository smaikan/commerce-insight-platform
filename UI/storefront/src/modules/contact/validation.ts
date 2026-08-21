import type {
  ContactDraft,
  ContactFieldErrors,
  ContactFieldName,
  ContactMessageSubject,
  ContactSubmissionRequest,
} from "@/modules/contact/types";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const SUBJECT_VALUES = new Set<ContactMessageSubject>([0, 1, 2, 3, 4, 5]);
const CONTACT_REQUEST_LIMIT_BYTES = 16 * 1024;

export type ContactValidationResult =
  | { ok: true; value: ContactSubmissionRequest; fingerprint: string }
  | { ok: false; errors: ContactFieldErrors; formError?: string };

function containsUnsafeText(value: string): boolean {
  return value.includes("\0") || value.includes("<") || value.includes(">") ||
    [...value].some((character) => {
      const code = character.charCodeAt(0);
      return code < 32 && character !== "\r" && character !== "\n" && character !== "\t";
    });
}

function addError(errors: ContactFieldErrors, field: ContactFieldName, message: string): void {
  errors[field] = [...(errors[field] ?? []), message];
}

// Burada browser ve BFF sınırının aynı alan kurallarını kullanıp API'ye yalnız normalize edilmiş contact gövdesi göndermesini sağlıyorum.
export function validateContactDraft(value: ContactDraft): ContactValidationResult {
  const errors: ContactFieldErrors = {};
  const name = value.name.trim();
  const email = value.email.trim().toLowerCase();
  const phone = value.phone.trim();
  const orderNumber = value.orderNumber.trim();
  const message = value.message.trim();

  if (name.length < 2) addError(errors, "name", "Adınız ve soyadınız en az 2 karakter olmalıdır.");
  else if (name.length > 150) addError(errors, "name", "Adınız ve soyadınız en fazla 150 karakter olabilir.");
  else if (containsUnsafeText(name)) addError(errors, "name", "Ad alanı yalnızca güvenli düz metin içerebilir.");

  if (!email) addError(errors, "email", "E-posta adresinizi girin.");
  else if (email.length > 320 || !EMAIL_PATTERN.test(email)) addError(errors, "email", "Geçerli bir e-posta adresi girin.");
  else if (containsUnsafeText(email)) addError(errors, "email", "E-posta adresi geçersiz karakter içeriyor.");

  if (phone.length > 30) addError(errors, "phone", "Telefon numarası en fazla 30 karakter olabilir.");
  else if (phone && containsUnsafeText(phone)) addError(errors, "phone", "Telefon numarası geçersiz karakter içeriyor.");

  if (!SUBJECT_VALUES.has(value.subject)) addError(errors, "subject", "Geçerli bir konu seçin.");

  if (orderNumber.length > 50) addError(errors, "orderNumber", "Sipariş numarası en fazla 50 karakter olabilir.");
  else if (orderNumber && containsUnsafeText(orderNumber)) addError(errors, "orderNumber", "Sipariş numarası geçersiz karakter içeriyor.");

  if (message.length < 20) addError(errors, "message", "Mesajınız en az 20 karakter olmalıdır.");
  else if (message.length > 5_000) addError(errors, "message", "Mesajınız en fazla 5000 karakter olabilir.");
  else if (containsUnsafeText(message)) addError(errors, "message", "Mesaj alanı HTML veya geçersiz kontrol karakteri içeremez.");

  if (Object.keys(errors).length > 0) return { ok: false, errors };

  const request: ContactSubmissionRequest = {
    name,
    email,
    phone: phone || null,
    subject: value.subject,
    orderNumber: orderNumber || null,
    message,
  };
  const fingerprint = JSON.stringify(request);
  if (new TextEncoder().encode(fingerprint).byteLength > CONTACT_REQUEST_LIMIT_BYTES) {
    return {
      ok: false,
      errors: { message: ["Mesaj içeriği istek boyutu sınırını aşıyor. Lütfen metni kısaltın."] },
      formError: "İletişim formu 16 KB istek sınırını aşıyor.",
    };
  }

  return { ok: true, value: request, fingerprint };
}

// Burada BFF'ye gelen bilinmeyen JSON değerini generated request biçimine dönüştürmeden önce daraltıyorum.
export function parseContactSubmission(value: unknown): ContactValidationResult {
  if (!value || typeof value !== "object") {
    return { ok: false, errors: {}, formError: "İletişim formu verileri geçersiz." };
  }

  const source = value as Record<string, unknown>;
  const subject = (typeof source.subject === "number" ? source.subject : -1) as ContactMessageSubject;
  return validateContactDraft({
    name: typeof source.name === "string" ? source.name : "",
    email: typeof source.email === "string" ? source.email : "",
    phone: typeof source.phone === "string" ? source.phone : "",
    subject,
    orderNumber: typeof source.orderNumber === "string" ? source.orderNumber : "",
    message: typeof source.message === "string" ? source.message : "",
  });
}

export function isSafeIdempotencyKey(value: string | null): value is string {
  const normalized = value?.trim() ?? "";
  return normalized.length > 0 && normalized.length <= 200 && ![...normalized].some((character) => character.charCodeAt(0) < 32);
}

export function parseContactTurnstileToken(value: string | null): string | undefined | null {
  if (!value) return undefined;
  const normalized = value.trim();
  return normalized && normalized.length <= 2_048 && ![...normalized].some((character) => character.charCodeAt(0) < 32)
    ? normalized
    : null;
}

export function mapApiFieldErrors(errors?: Record<string, string[]>): ContactFieldErrors {
  const mapped: ContactFieldErrors = {};
  if (!errors) return mapped;

  const fields: ContactFieldName[] = ["name", "email", "phone", "subject", "orderNumber", "message"];
  for (const [key, messages] of Object.entries(errors)) {
    const normalizedKey = key.split(".").at(-1)?.toLowerCase();
    const field = fields.find((candidate) => candidate.toLowerCase() === normalizedKey);
    if (field) mapped[field] = messages;
  }
  return mapped;
}
