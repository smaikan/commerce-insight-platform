import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { buildCurrentAccountListHref } from "@/modules/accounting/current-accounts/query";
import type { CurrentAccountListQuery, CurrentAccountPage } from "@/modules/accounting/current-accounts/types";

export function CurrentAccountPagination({ page, query }: { page: CurrentAccountPage; query: CurrentAccountListQuery }) {
  return (
    <AdminPagination
      action="/accounting/current-accounts"
      ariaLabel="Cari hesap sayfalama"
      buildHref={(pageNumber) => buildCurrentAccountListHref(query, pageNumber)}
      hiddenFields={query.pageSize !== 20 ? [{ name: "pageSize", value: query.pageSize }] : []}
      itemLabel="cari hesap"
      pageNumber={page.pageNumber}
      pageSize={page.pageSize}
      totalCount={page.totalCount}
      totalPages={page.totalPages}
    />
  );
}
