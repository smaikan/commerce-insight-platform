import type { CouponRequest } from "@/modules/coupons/types";

type CouponParseResult = { ok: true; value: CouponRequest } | { ok: false; message: string; fieldErrors: Record<string, string[]> };

// Burada kupon formunu API'ye göndermeden önce temel biçim, tarih ve sayı kurallarıyla doğruluyorum.
export function parseCouponForm(formData: FormData): CouponParseResult {
  const code = text(formData, "code").toUpperCase();
  const description = text(formData, "description");
  const discountType = integer(formData, "discountType");
  const discountValue = number(formData, "discountValue");
  const minimumOrderAmount = optionalNumber(formData, "minimumOrderAmount");
  const usageLimit = optionalInteger(formData, "usageLimit");
  const startsAt = optionalDateTime(formData, "startsAt");
  const expiresAt = optionalDateTime(formData, "expiresAt");
  const fieldErrors: Record<string, string[]> = {};

  if (!code) addError(fieldErrors, "code", "Kupon kodu zorunludur.");
  if (code.length > 100) addError(fieldErrors, "code", "Kupon kodu en fazla 100 karakter olabilir.");
  if (description.length > 500) addError(fieldErrors, "description", "Açıklama en fazla 500 karakter olabilir.");
  if (discountType !== 0 && discountType !== 1) addError(fieldErrors, "discountType", "Geçerli bir indirim türü seçin.");
  if (discountValue === undefined || discountValue <= 0) addError(fieldErrors, "discountValue", "İndirim değeri sıfırdan büyük olmalıdır.");
  if (discountType === 0 && discountValue !== undefined && discountValue > 100) addError(fieldErrors, "discountValue", "Yüzde indirimi 100'ü aşamaz.");
  if (minimumOrderAmount !== undefined && minimumOrderAmount < 0) addError(fieldErrors, "minimumOrderAmount", "Minimum sepet tutarı negatif olamaz.");
  if (usageLimit !== undefined && usageLimit <= 0) addError(fieldErrors, "usageLimit", "Kullanım limiti pozitif tam sayı olmalıdır.");
  if (startsAt === "invalid") addError(fieldErrors, "startsAt", "Başlangıç tarihi geçerli değil.");
  if (expiresAt === "invalid") addError(fieldErrors, "expiresAt", "Bitiş tarihi geçerli değil.");
  if (startsAt !== "invalid" && expiresAt !== "invalid" && startsAt && expiresAt && startsAt >= expiresAt) addError(fieldErrors, "expiresAt", "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
  if (Object.keys(fieldErrors).length) return { ok: false, message: "Kupon bilgilerini kontrol edin.", fieldErrors };

  return {
    ok: true,
    value: {
      code,
      description: description || null,
      discountType: discountType as 0 | 1,
      discountValue: discountValue as number,
      minimumOrderAmount: minimumOrderAmount ?? null,
      usageLimit: usageLimit ?? null,
      startsAt: startsAt || null,
      expiresAt: expiresAt || null,
      isMemberOnly: formData.get("isMemberOnly") === "on",
      isActive: formData.get("isActive") === "on",
    },
  };
}

// Burada metin alanlarını tek biçimde kırpıyorum.
function text(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value.trim() : "";
}

// Burada zorunlu sayısal alanın yalnız sonlu sayı olup olmadığını ayırıyorum.
function number(formData: FormData, name: string): number | undefined {
  const value = text(formData, name);
  if (!value) return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

// Burada opsiyonel tutarı boş bırakıldığında null API alanına dönüştürüyorum.
function optionalNumber(formData: FormData, name: string): number | undefined {
  return number(formData, name);
}

// Burada yalnız tam sayı alanlarını kabul ediyorum.
function integer(formData: FormData, name: string): number | undefined {
  const value = number(formData, name);
  return value !== undefined && Number.isInteger(value) ? value : undefined;
}

// Burada opsiyonel tam sayı kullanım limitini ayırıyorum.
function optionalInteger(formData: FormData, name: string): number | undefined {
  return integer(formData, name);
}

// Burada datetime-local değerini geçerli UTC ISO tarihine çeviriyorum.
function optionalDateTime(formData: FormData, name: string): string | "invalid" | undefined {
  const value = text(formData, name);
  if (!value) return undefined;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "invalid" : date.toISOString();
}

// Burada aynı alandaki birden çok hata mesajını koruyorum.
function addError(errors: Record<string, string[]>, field: string, message: string) {
  errors[field] = [...(errors[field] || []), message];
}
