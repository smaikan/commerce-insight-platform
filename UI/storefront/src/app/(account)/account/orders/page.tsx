import type { Metadata } from "next";

import { getAccountOrders } from "@/modules/account/api";
import { OrdersView } from "@/modules/account/components/orders-view";
import { withAccountSession } from "@/modules/account/session";

export const metadata: Metadata = { title: "Siparişlerim" };

type OrdersPageProps = { searchParams: Promise<{ page?: string; status?: string }> };

// Burada paylaşılabilir sipariş filtresi ve pagination durumunu URL parametrelerinden güvenli aralıklara indiriyorum.
export default async function AccountOrdersPage({ searchParams }: OrdersPageProps) {
  const params = await searchParams;
  const parsedPage = Number(params.page);
  const pageNumber = Number.isInteger(parsedPage) && parsedPage > 0 ? Math.min(parsedPage, 10_000) : 1;
  const parsedStatus = Number(params.status);
  const status = params.status !== undefined && params.status !== "" && Number.isInteger(parsedStatus) && parsedStatus >= 0 && parsedStatus <= 9
    ? parsedStatus
    : undefined;
  const orders = await withAccountSession("/account/orders", () => getAccountOrders({ pageNumber, pageSize: 10, status }));
  return <OrdersView orders={orders} status={status} />;
}
