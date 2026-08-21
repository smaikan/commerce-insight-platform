import type { OrderStatus, PaymentProvider, PaymentStatus } from "@/modules/orders/types";

// Burada backend OrderStatus enumunun bütün numeric değerlerini Türkçe operatör etiketleriyle eşliyorum.
export const orderStatusOptions: Array<{ value: OrderStatus; label: string }> = [
  { value: 0, label: "Onay Bekliyor" },
  { value: 1, label: "Onaylandı" },
  { value: 2, label: "Ödeme Alındı" },
  { value: 3, label: "Hazırlanıyor" },
  { value: 4, label: "Kargoya Verildi" },
  { value: 5, label: "Teslim Edildi" },
  { value: 6, label: "İptal Edildi" },
  { value: 7, label: "Ücret İade Edildi" },
  { value: 8, label: "İade Talebi Oluşturuldu" },
  { value: 9, label: "İade Talebi Onaylandı" },
];

// Burada sipariş yaşam döngüsünün gerçek durumlarını sınırlı semantik rozet rolleriyle eşliyorum.
const orderStatusClasses: Record<OrderStatus, string> = {
  0: "border-yellow-200 bg-yellow-50 text-yellow-800", // Onay Bekliyor (Warning/Yellow)
  1: "border-blue-200 bg-blue-50 text-blue-800", // Onaylandı (Info/Blue)
  2: "border-emerald-200 bg-emerald-50 text-emerald-800", // Ödeme Alındı (Success)
  3: "border-amber-200 bg-amber-50 text-amber-800", // Hazırlanıyor (Warning)
  4: "border-indigo-200 bg-indigo-50 text-indigo-800", // Kargoya Verildi (Primary)
  5: "border-emerald-300 bg-emerald-100 text-emerald-900", // Teslim Edildi (Success Strong)
  6: "border-red-200 bg-red-50 text-red-800", // İptal Edildi (Danger)
  7: "border-rose-200 bg-rose-50 text-rose-800", // Ücret İade Edildi (Danger variant)
  8: "border-orange-200 bg-orange-50 text-orange-800", // İade Talebi Oluşturuldu (Warning variant)
  9: "border-teal-200 bg-teal-50 text-teal-800", // İade Talebi Onaylandı (Success variant)
};

// Burada ödeme durum etiketlerini e-ticaret PaymentStatus sözleşmesine göre tanımlıyorum.
const paymentStatusLabels: Record<PaymentStatus, string> = {
  0: "Bekliyor",
  1: "Ödendi",
  2: "Başarısız",
  3: "İade edildi",
  4: "İptal edildi",
};

// Burada ödeme sonucu rozetlerini bekleme, başarı, hata ve terminal durumlarına göre ayırıyorum.
const paymentStatusClasses: Record<PaymentStatus, string> = {
  0: "border-amber-200 bg-amber-50 text-amber-800",
  1: "border-emerald-200 bg-emerald-50 text-emerald-800",
  2: "border-red-200 bg-red-50 text-red-800",
  3: "border-slate-300 bg-slate-100 text-slate-700",
  4: "border-slate-300 bg-slate-100 text-slate-700",
};

// Burada ödeme sağlayıcısı enum değerlerini kullanıcıya gösterilecek adlarla eşliyorum.
const paymentProviderLabels: Record<PaymentProvider, string> = {
  0: "Test sağlayıcısı",
  1: "Iyzico",
  2: "Stripe",
  3: "PayTR",
};

// Burada tarih ve tutar gösterimini bütün sipariş bileşenlerinde tutarlı formatlayıcılarla hazırlıyorum.
const dateTimeFormatter = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
  timeStyle: "short",
  timeZone: "Europe/Istanbul",
});

const moneyFormatter = new Intl.NumberFormat("tr-TR", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

// Burada numeric sipariş durumunu kullanıcıya gösterilecek belgelenmiş etikete dönüştürüyorum.
export function orderStatusLabel(status: OrderStatus): string {
  return orderStatusOptions.find((option) => option.value === status)?.label || "Bilinmiyor";
}

// Burada sipariş durumunun semantik rozet görünümünü tek kaynaktan yönetiyorum.
export function orderStatusClass(status: OrderStatus): string {
  return orderStatusClasses[status];
}

// Burada ödeme durumunu API enum değerinden okunabilir etikete dönüştürüyorum.
export function paymentStatusLabel(status: PaymentStatus): string {
  return paymentStatusLabels[status];
}

// Burada ödeme durumunu yalnız gerçek semantik anlamında renklendiriyorum.
export function paymentStatusClass(status: PaymentStatus): string {
  return paymentStatusClasses[status];
}

// Burada ödeme sağlayıcısını backend enum değeriyle eşleşen güvenli adıyla gösteriyorum.
export function paymentProviderLabel(provider: PaymentProvider): string {
  return paymentProviderLabels[provider];
}

// Burada backend parasal değerini para birimi varsaymadan Türkçe sayı biçiminde gösteriyorum.
export function formatOrderAmount(value: number): string {
  return moneyFormatter.format(value);
}

// Burada UTC tarihleri yönetim ekranında tutarlı Türkiye saatiyle gösteriyorum.
export function formatOrderDate(value?: string | null): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : dateTimeFormatter.format(date);
}
