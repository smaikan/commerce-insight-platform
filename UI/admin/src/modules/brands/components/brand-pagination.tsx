import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { buildBrandListHref } from "@/modules/brands/query";
import type { BrandListQuery, BrandPage } from "@/modules/brands/types";

// Burada marka listesini ortak admin sayfalama düzenine bağlayıp seçili sayfa boyutunu doğrudan atlamada koruyorum.
export function BrandPagination({ page, query }: { page: BrandPage; query: BrandListQuery }) {
  return (
    <AdminPagination
      action="/brands"
      ariaLabel="Marka listesi sayfalama"
      buildHref={(pageNumber) => buildBrandListHref(query, pageNumber)}
      hiddenFields={query.pageSize !== 20 ? [{ name: "pageSize", value: query.pageSize }] : []}
      itemLabel="marka"
      pageNumber={page.pageNumber}
      pageSize={page.pageSize}
      totalCount={page.totalCount}
      totalPages={page.totalPages}
    />
  );
}
