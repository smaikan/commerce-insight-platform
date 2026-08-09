import Link from "next/link";
import { buildCustomerListHref } from "@/modules/customers/query";
import type { CustomerListQuery, UserPage } from "@/modules/customers/types";

// Burada sunucu sayfalamasını mevcut müşteri filtrelerini koruyan önceki, sonraki ve doğrudan sayfa atlama kontrolleriyle sunuyorum.
export function CustomerPagination({
  page,
  query,
}: {
  page: UserPage;
  query: CustomerListQuery;
}) {
  const firstItem = page.totalCount === 0 ? 0 : (page.pageNumber - 1) * page.pageSize + 1;
  const lastItem = Math.min(page.pageNumber * page.pageSize, page.totalCount);

  return (
    <footer className="flex flex-col gap-3 border-t border-border bg-surface-subtle/40 px-4 py-3 text-sm text-muted sm:flex-row sm:items-center sm:justify-between sm:px-5">
      <p>
        <span className="font-medium text-foreground">
          {firstItem}-{lastItem}
        </span>{" "}
        / {page.totalCount} müşteri
      </p>
      <nav aria-label="Müşteri listesi sayfalama" className="flex items-center gap-2">
        {page.hasPreviousPage ? (
          <Link
            href={buildCustomerListHref(query, page.pageNumber - 1)}
            className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 font-medium text-foreground hover:bg-surface-subtle"
          >
            Önceki
          </Link>
        ) : (
          <span
            aria-disabled="true"
            className="inline-flex min-h-10 items-center rounded-lg border border-border bg-surface px-3 text-muted"
          >
            Önceki
          </span>
        )}
        <span className="min-w-20 text-center">
          {page.pageNumber} / {Math.max(page.totalPages, 1)}
        </span>
        <form action="/customers" method="get" className="flex items-center gap-1.5">
          {query.pageSize !== 20 ? <input type="hidden" name="pageSize" value={query.pageSize} /> : null}
          {query.search ? <input type="hidden" name="search" value={query.search} /> : null}
          {query.status !== undefined ? <input type="hidden" name="status" value={query.status} /> : null}
          <label className="sr-only" htmlFor="customer-page-number">Sayfa numarasına git</label>
          <input
            id="customer-page-number"
            name="pageNumber"
            type="number"
            min={1}
            max={Math.max(page.totalPages, 1)}
            defaultValue={page.pageNumber}
            className="min-h-10 w-16 rounded-lg border border-border-strong bg-surface-strong px-2 text-center text-sm font-medium text-foreground outline-none focus:border-primary sm:min-h-9"
          />
          <button type="submit" className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-2.5 text-xs font-semibold text-foreground hover:bg-surface-subtle sm:min-h-9">
            Git
          </button>
        </form>
        {page.hasNextPage ? (
          <Link
            href={buildCustomerListHref(query, page.pageNumber + 1)}
            className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 font-medium text-foreground hover:bg-surface-subtle"
          >
            Sonraki
          </Link>
        ) : (
          <span
            aria-disabled="true"
            className="inline-flex min-h-10 items-center rounded-lg border border-border bg-surface px-3 text-muted"
          >
            Sonraki
          </span>
        )}
      </nav>
    </footer>
  );
}
