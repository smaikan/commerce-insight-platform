import type { Metadata } from "next";
import { notFound } from "next/navigation";

import { ApiError } from "@/lib/api/problem";
import { getAccountOrder } from "@/modules/account/api";
import { OrderDetail } from "@/modules/account/components/order-detail";
import { withAccountSession } from "@/modules/account/session";

export const metadata: Metadata = { title: "Sipariş Detayı" };

type OrderDetailPageProps = { params: Promise<{ orderId: string }> };

// Burada kullanıcıya ait olmayan veya bulunmayan siparişi ayrıntı sızdırmadan 404 davranışına taşıyorum.
export default async function AccountOrderDetailPage({ params }: OrderDetailPageProps) {
  const { orderId } = await params;
  let order;
  try {
    order = await withAccountSession(`/account/orders/${orderId}`, () => getAccountOrder(orderId));
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }
  return <OrderDetail order={order} />;
}
