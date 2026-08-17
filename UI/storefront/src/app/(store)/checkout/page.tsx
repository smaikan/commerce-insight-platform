import type { Metadata } from "next";

import { siteConfig } from "@/lib/site-config";
import { hasAuthSessionCookie } from "@/lib/auth/cookies";
import { getAccountAddresses } from "@/modules/account/api";
import { withAccountSession } from "@/modules/account/session";
import { getActiveShippingMethods } from "@/modules/checkout/api";
import { CheckoutForm } from "@/modules/checkout/components/checkout-form";
import { isCheckoutOrderCreationEnabled } from "@/modules/checkout/config";

export const metadata: Metadata = {
  title: "Siparişi Tamamla",
  description: "Teslimat ve iletişim bilgilerinizi tamamlayın.",
  robots: { index: false, follow: false },
};

// Burada public kargo seçeneklerini sunucuda okuyup kişisel checkout taslağını en küçük client formuna bırakıyorum.
export default async function CheckoutPage() {
  const [shippingMethods, hasAccountSession] = await Promise.all([getActiveShippingMethods(), hasAuthSessionCookie()]);
  // Burada üye checkout'unda yalnız sahiplik denetimli kayıtlı adresleri sunucu tarafında okuyup guest formundan kesin olarak ayırıyorum.
  const accountAddresses = hasAccountSession
    ? await withAccountSession("/checkout", () => getAccountAddresses())
    : null;
  // Burada public site key'i yalnız checkout client sınırına geçirip Turnstile secret'ını API tarafında bırakıyorum.
  const turnstileSiteKey = process.env.TURNSTILE_SITE_KEY?.trim() || "";
  return (
    <CheckoutForm
      shippingMethods={shippingMethods}
      currency={siteConfig.currency}
      turnstileSiteKey={turnstileSiteKey}
      orderCreationEnabled={isCheckoutOrderCreationEnabled()}
      accountAddresses={accountAddresses}
    />
  );
}
