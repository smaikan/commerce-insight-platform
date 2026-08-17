import "server-only";

// Burada sipariş oluşturmayı yalnız açıkça etkinleştirilmiş ortamlarda açıp canlı yapılandırmada güvenli biçimde kapalı tutuyorum.
export function isCheckoutOrderCreationEnabled(): boolean {
  return process.env.CHECKOUT_ORDER_CREATION_ENABLED?.trim().toLowerCase() === "true";
}
