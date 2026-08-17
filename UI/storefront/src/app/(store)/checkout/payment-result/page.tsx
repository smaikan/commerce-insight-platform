import type { Metadata } from "next";
import { notFound } from "next/navigation";

import { isUuid } from "@/lib/validation/identifiers";
import { PaymentResult } from "@/modules/checkout/components/payment-result";

export const metadata: Metadata = {
  title: "Ödeme sonucu",
  robots: { index: false, follow: false },
};

type PaymentResultPageProps = {
  searchParams: Promise<{ orderId?: string | string[]; paymentId?: string | string[]; status?: string | string[] }>;
};

// Burada callback query'sinden yalnız geçerli orderId'yi rota ipucu olarak alıyor, paymentId ve status değerlerini ödeme kararı için kullanmıyorum.
export default async function PaymentResultPage({ searchParams }: PaymentResultPageProps) {
  const query = await searchParams;
  const orderId = typeof query.orderId === "string" ? query.orderId : "";
  if (!isUuid(orderId)) notFound();
  return <PaymentResult orderId={orderId} />;
}
