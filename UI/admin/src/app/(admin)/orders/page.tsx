import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getOrders } from "@/modules/orders/api";
import { OrderFilters } from "@/modules/orders/components/order-filters";
import { OrderPagination } from "@/modules/orders/components/order-pagination";
import { OrderTable } from "@/modules/orders/components/order-table";
import { buildOrderListHref, parseOrderListQuery } from "@/modules/orders/query";

export const metadata: Metadata = { title: "Siparişler" };

// Burada URL tabanlı belgelenmiş filtreleri okuyup yönetici sipariş listesini server-side hazırlıyorum.
export default async function OrdersPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const query = parseOrderListQuery(await searchParams);
  const session = await requireAdminPageSession(buildOrderListHref(query, query.pageNumber));
  const page = await getOrders(query, session);

  return (
    <div className="w-full">
      <PageHeader
        title="Siparişler"
        description="E-ticaret siparişlerini durum ve oluşturulma tarihine göre filtreleyin; kalem, ödeme ve teslimat snapshot'larını inceleyin."
      />

      <section aria-label="Sipariş listesi" className="overflow-hidden rounded-xl border border-border bg-surface">
        <OrderFilters query={query} />
        <OrderTable page={page} query={query} />
        <OrderPagination page={page} query={query} />
      </section>
    </div>
  );
}
