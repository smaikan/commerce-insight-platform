import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { buildCustomerListHref } from "@/modules/customers/query";
import type { CustomerListQuery, UserPage } from "@/modules/customers/types";

// Burada müşteri filtrelerini kaybetmeden ortak admin sayfalama ve doğrudan sayfa atlama düzenini kullanıyorum.
export function CustomerPagination({ page, query }: { page: UserPage; query: CustomerListQuery }) {
  const hiddenFields = [
    ...(query.pageSize !== 20 ? [{ name: "pageSize", value: query.pageSize }] : []),
    ...(query.search ? [{ name: "search", value: query.search }] : []),
    ...(query.status !== undefined ? [{ name: "status", value: query.status }] : []),
  ];

  return (
    <AdminPagination
      action="/customers"
      ariaLabel="Müşteri listesi sayfalama"
      buildHref={(pageNumber) => buildCustomerListHref(query, pageNumber)}
      hiddenFields={hiddenFields}
      itemLabel="müşteri"
      pageNumber={page.pageNumber}
      pageSize={page.pageSize}
      totalCount={page.totalCount}
      totalPages={page.totalPages}
    />
  );
}
