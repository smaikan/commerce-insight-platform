import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { buildOrderListHref } from "@/modules/orders/query";
import type { OrderListQuery, OrderPage } from "@/modules/orders/types";

// Burada sipariş filtrelerini kaybetmeden ortak admin sayfalama ve doğrudan sayfa atlama düzenini kullanıyorum.
export function OrderPagination({ page, query }: { page: OrderPage; query: OrderListQuery }) {
  const hiddenFields = [
    ...(query.pageSize !== 20 ? [{ name: "pageSize", value: query.pageSize }] : []),
    ...(query.search ? [{ name: "search", value: query.search }] : []),
    ...(query.status !== undefined ? [{ name: "status", value: query.status }] : []),
    ...(query.createdFrom ? [{ name: "createdFrom", value: query.createdFrom }] : []),
    ...(query.createdTo ? [{ name: "createdTo", value: query.createdTo }] : []),
  ];

  return (
    <AdminPagination
      action="/orders"
      ariaLabel="Sipariş listesi sayfalama"
      buildHref={(pageNumber) => buildOrderListHref(query, pageNumber)}
      hiddenFields={hiddenFields}
      itemLabel="sipariş"
      pageNumber={page.pageNumber}
      pageSize={page.pageSize}
      totalCount={page.totalCount}
      totalPages={page.totalPages}
    />
  );
}
