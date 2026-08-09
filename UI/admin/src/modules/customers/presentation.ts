import type { UserRole, UserStatus } from "@/modules/customers/types";

// Burada backend UserRole enum'unun numeric değerlerini Türkçe operatör etiketleriyle eşliyorum.
export const userRoleOptions: Array<{ value: UserRole; label: string }> = [
  { value: 1, label: "Müşteri" },
  { value: 2, label: "Yönetici" },
];

// Burada backend UserStatus enum'unun numeric değerlerini Türkçe operatör etiketleriyle eşliyorum.
export const userStatusOptions: Array<{ value: UserStatus; label: string }> = [
  { value: 1, label: "Aktif" },
  { value: 2, label: "Pasif" },
  { value: 3, label: "Silindi" },
];

// Burada kullanıcı rolünü API enum değerinden okunabilir etikete dönüştürüyorum.
export function userRoleLabel(role: UserRole): string {
  return userRoleOptions.find((o) => o.value === role)?.label ?? "Bilinmiyor";
}

// Burada kullanıcı durumunu API enum değerinden okunabilir etikete dönüştürüyorum.
export function userStatusLabel(status: UserStatus): string {
  return userStatusOptions.find((o) => o.value === status)?.label ?? "Bilinmiyor";
}

// Burada kullanıcı durumunu yalnız gerçek semantik anlamında renklendiriyorum.
export function userStatusClass(status: UserStatus): string {
  const map: Record<UserStatus, string> = {
    1: "border-emerald-200 bg-emerald-50 text-emerald-800",
    2: "border-amber-200 bg-amber-50 text-amber-800",
    3: "border-red-200 bg-red-50 text-red-800",
  };
  return map[status] ?? "border-slate-300 bg-slate-100 text-slate-700";
}

// Burada kullanıcı rolünü compact rozet rengiyle ayırt ediyorum.
export function userRoleClass(role: UserRole): string {
  const map: Record<UserRole, string> = {
    1: "border-slate-200 bg-slate-50 text-slate-700",
    2: "border-blue-200 bg-blue-50 text-blue-800",
  };
  return map[role] ?? "border-slate-200 bg-slate-50 text-slate-700";
}

// Burada UTC tarihleri yönetim ekranında tutarlı Türkiye saatiyle gösteriyorum.
const dateTimeFormatter = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
  timeStyle: "short",
  timeZone: "Europe/Istanbul",
});

const dateOnlyFormatter = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
  timeZone: "Europe/Istanbul",
});

export function formatUserDate(value?: string | null): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : dateTimeFormatter.format(date);
}

export function formatUserDateOnly(value?: string | null): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : dateOnlyFormatter.format(date);
}
