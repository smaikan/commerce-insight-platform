import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { buildStockMovementListHref } from "@/modules/inventory/query";
import type { StockMovementListQuery, StockMovementPage } from "@/modules/inventory/types";

// Burada stok defteri filtrelerini koruyarak ortak admin sayfalama ve doğrudan sayfa atlama düzenini kullanıyorum.
export function StockMovementPagination({ page, query }: { page: StockMovementPage; query: StockMovementListQuery }) {
  const hiddenFields = [
    ...(query.pageSize !== 20 ? [{ name: "pageSize", value: query.pageSize }] : []),
    ...(query.search ? [{ name: "search", value: query.search }] : []),
    ...(query.productVariantId ? [{ name: "productVariantId", value: query.productVariantId }] : []),
    ...(query.direction ? [{ name: "direction", value: query.direction }] : []),
    ...(query.type ? [{ name: "type", value: query.type }] : []),
    ...(query.createdFrom ? [{ name: "createdFrom", value: query.createdFrom }] : []),
    ...(query.createdTo ? [{ name: "createdTo", value: query.createdTo }] : []),
    ...(query.balanceVariantId ? [{ name: "balanceVariantId", value: query.balanceVariantId }] : []),
  ];

  return (
    <AdminPagination
      action="/inventory/stock-movements"
      ariaLabel="Stok hareketleri sayfalama"
      buildHref={(pageNumber) => buildStockMovementListHref(query, pageNumber)}
      hiddenFields={hiddenFields}
      itemLabel="hareket"
      pageNumber={page.pageNumber}
      pageSize={page.pageSize}
      totalCount={page.totalCount}
      totalPages={page.totalPages}
    />
  );
}
