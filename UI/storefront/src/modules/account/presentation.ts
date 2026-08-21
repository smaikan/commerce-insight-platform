const DATE_FORMATTER = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "long",
  year: "numeric",
  timeZone: "Europe/Istanbul",
});

const DATE_TIME_FORMATTER = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "long",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  timeZone: "Europe/Istanbul",
});

export const ORDER_STATUS_LABELS: Record<number, string> = {
  0: "Onay Bekliyor",
  1: "Onaylandı",
  2: "Ödeme Alındı",
  3: "Hazırlanıyor",
  4: "Kargoya Verildi",
  5: "Teslim Edildi",
  6: "İptal Edildi",
  7: "Ücret İade Edildi",
  8: "İade Talebi Oluşturuldu",
  9: "İade Talebi Onaylandı",
};

// Burada API tarihini mağaza dilinde, sabit Türkiye saat dilimiyle sunuyorum.
export function formatAccountDate(value: string): string {
  return DATE_FORMATTER.format(new Date(value));
}

// Burada kargo hareketlerinin saat bilgisini kaybetmeden kullanıcıya okunabilir tarih üretiyorum.
export function formatAccountDateTime(value: string): string {
  return DATE_TIME_FORMATTER.format(new Date(value));
}

// Burada bilinmeyen enum değerini yanlış bir müşteri durumuna çevirmeden güvenli etiketliyorum.
export function orderStatusLabel(status: number): string {
  return ORDER_STATUS_LABELS[status] ?? "Durum güncelleniyor";
}

// Burada yalnız dolu snapshot slug'ından sözleşmedeki güvenli Storefront ürün yolunu kuruyorum.
export function orderItemHref(productUrl: string | null | undefined): string | null {
  const slug = productUrl?.trim();
  return slug ? `/products/${encodeURIComponent(slug)}` : null;
}

// Burada API'nin URI alanını kullanıcıya açmadan önce yalnız mutlak HTTP/HTTPS protokolüyle sınırlandırıyorum.
export function safeTrackingUrl(value: string | null | undefined): string | null {
  if (!value) return null;
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:" ? url.toString() : null;
  } catch {
    return null;
  }
}
