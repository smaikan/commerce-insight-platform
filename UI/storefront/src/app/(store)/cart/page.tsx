import type { Metadata } from "next";

import { siteConfig } from "@/lib/site-config";
import { CartView } from "@/modules/cart/components/cart-view";

export const metadata: Metadata = {
  title: "Sepet",
  description: "Sepetinizdeki ürünleri ve adetlerini yönetin.",
  robots: { index: false, follow: false },
};

// Burada kişisel sepet verisini HTML cache'ine taşımadan yalnızca sayfa kabuğunu sunucuda oluşturuyorum.
export default function CartPage() {
  return <CartView currency={siteConfig.currency} />;
}
