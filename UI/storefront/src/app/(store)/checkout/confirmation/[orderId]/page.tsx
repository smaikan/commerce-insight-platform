import type { Metadata } from "next";
import { notFound } from "next/navigation";

import { isUuid } from "@/lib/validation/identifiers";
import { siteConfig } from "@/lib/site-config";
import { hasAuthSessionCookie } from "@/lib/auth/cookies";
import { OrderConfirmation } from "@/modules/checkout/components/order-confirmation";

export const metadata: Metadata = {
  title: "Sipariş onayı",
  robots: { index: false, follow: false },
};

type ConfirmationPageProps = {
  params: Promise<{ orderId: string }>;
};

// Burada kişisel sipariş verisini server HTML'ine veya shared cache'e taşımadan yalnız rota kimliğini client confirmation sınırına veriyorum.
export default async function ConfirmationPage({ params }: ConfirmationPageProps) {
  const { orderId } = await params;
  if (!isUuid(orderId)) notFound();
  const isMember = await hasAuthSessionCookie();
  return <OrderConfirmation orderId={orderId} currency={siteConfig.currency} isMember={isMember} />;
}
