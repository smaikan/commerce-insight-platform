import "server-only";

export type SandboxPaymentInfo = Readonly<{
  cardNumber: string;
}>;

const SANDBOX_TEST_CARD_NUMBER = "4543590000000006";

// Burada sipariş oluşturmayı yalnız açıkça etkinleştirilmiş ortamlarda açıp canlı yapılandırmada güvenli biçimde kapalı tutuyorum.
export function isCheckoutOrderCreationEnabled(): boolean {
  return process.env.CHECKOUT_ORDER_CREATION_ENABLED?.trim().toLowerCase() === "true";
}

// Burada test kartını yalnız açıkça sandbox olarak işaretlenen storefront ortamına geçirip diğer tüm yapılandırmalarda kapalı tutuyorum.
export function getSandboxPaymentInfo(): SandboxPaymentInfo | null {
  if (process.env.CHECKOUT_PAYMENT_ENVIRONMENT?.trim().toLowerCase() !== "sandbox") return null;

  return { cardNumber: SANDBOX_TEST_CARD_NUMBER };
}
