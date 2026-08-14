import "server-only";

// Burada sipariş oluşturmayı yalnız açıkça etkinleştirilmiş ortamlarda açıp canlı yapılandırmada güvenli biçimde kapalı tutuyorum.
export function isCheckoutOrderCreationEnabled(): boolean {
  // Burada ödeme entegrasyonu tamamlanana kadar production build'inde ortam değişkeni yanlışlıkla açılsa bile mutation kapısını kapalı tutuyorum.
  if (process.env.NODE_ENV === "production") return false;
  return process.env.CHECKOUT_ORDER_CREATION_ENABLED?.trim().toLowerCase() === "true";
}
