import type { CurrentAccountFormDraft, CurrentAccountFormState, CurrentAccountInput, CurrentAccountType } from "./types";

export function parseCurrentAccountForm(formData: FormData): { ok: true; input: CurrentAccountInput; draft: CurrentAccountFormDraft } | { ok: false; state: CurrentAccountFormState } {
  const draft = currentAccountFormDraft(formData);
  const typeValue = Number(draft.type);
  const type = ([1, 2, 3] as number[]).includes(typeValue) ? typeValue as CurrentAccountType : null;
  const values = {
    code: draft.code, name: draft.name, tradeName: optional(draft.tradeName),
    nationalIdentityNumber: optional(draft.nationalIdentityNumber), taxNumber: optional(draft.taxNumber), taxOffice: optional(draft.taxOffice),
    phoneNumber: optional(draft.phoneNumber), email: optional(draft.email), country: optional(draft.country), city: optional(draft.city),
    district: optional(draft.district), neighborhood: optional(draft.neighborhood), addressLine: optional(draft.addressLine), postalCode: optional(draft.postalCode), userId: optional(draft.userId),
  };
  const fieldErrors: Record<string, string[]> = {};
  check(fieldErrors, "code", values.code, 1, 50, "Cari kodu");
  check(fieldErrors, "name", values.name, 1, 250, "Cari unvanı");
  if (!type) fieldErrors.type = ["Geçerli bir cari türü seçin."];
  const limits: Array<[keyof typeof values, number, string]> = [["tradeName", 250, "Ticari unvan"], ["nationalIdentityNumber", 20, "Kimlik numarası"], ["taxNumber", 20, "Vergi numarası"], ["taxOffice", 100, "Vergi dairesi"], ["phoneNumber", 30, "Telefon"], ["email", 320, "E-posta"], ["country", 150, "Ülke"], ["city", 150, "Şehir"], ["district", 150, "İlçe"], ["neighborhood", 150, "Mahalle"], ["addressLine", 500, "Adres"], ["postalCode", 20, "Posta kodu"]];
  for (const [key, max, label] of limits) if ((values[key]?.length ?? 0) > max) fieldErrors[key] = [`${label} en fazla ${max} karakter olabilir.`];
  if (values.email && !values.email.includes("@")) fieldErrors.email = ["Geçerli bir e-posta adresi girin."];
  if (values.userId && !isCanonicalUserPublicId(values.userId)) fieldErrors.userId = ["Kullanıcı ID değeri U ile başlayan kanonik 6–8 karakterlik public ID olmalıdır."];
  if (Object.keys(fieldErrors).length) return { ok: false, state: { status: "error", message: "İşaretli alanları kontrol edin.", fieldErrors, draft } };
  return { ok: true, input: { ...values, type: type! }, draft };
}

export function currentAccountFormDraft(data: FormData): CurrentAccountFormDraft {
  return {
    code: text(data, "code"), type: text(data, "type"), name: text(data, "name"), tradeName: text(data, "tradeName"),
    nationalIdentityNumber: text(data, "nationalIdentityNumber"), taxNumber: text(data, "taxNumber"), taxOffice: text(data, "taxOffice"),
    phoneNumber: text(data, "phoneNumber"), email: text(data, "email"), country: text(data, "country"), city: text(data, "city"),
    district: text(data, "district"), neighborhood: text(data, "neighborhood"), addressLine: text(data, "addressLine"), postalCode: text(data, "postalCode"), userId: text(data, "userId"),
    isActive: data.get("isActive") === "on",
  };
}

export function isCanonicalUserPublicId(value: string): boolean {
  if (!/^U[0-9A-Z]{5,7}$/.test(value)) return false;
  const payload = value.slice(1);
  let decoded = 0;
  for (const character of payload) decoded = (decoded * 36) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".indexOf(character);
  if (decoded <= 0 || decoded > 78_364_164_095) return false;
  return `U${decoded.toString(36).toUpperCase().padStart(5, "0")}` === value;
}

function text(data: FormData, name: string): string { const value = data.get(name); return typeof value === "string" ? value.trim() : ""; }
function optional(value: string): string | null { return value || null; }
function check(errors: Record<string, string[]>, key: string, value: string, min: number, max: number, label: string): void { if (value.length < min || value.length > max) errors[key] = [`${label} ${min}–${max} karakter olmalıdır.`]; }
