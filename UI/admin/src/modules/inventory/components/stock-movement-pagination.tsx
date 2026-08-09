import Link from "next/link";
import { buildStockMovementListHref } from "@/modules/inventory/query";
import type { StockMovementListQuery, StockMovementPage } from "@/modules/inventory/types";

// Burada stok defterinin sayfalamasını mevcut filtreleri kaybetmeden sunuyorum.
export function StockMovementPagination({ page, query }: { page: StockMovementPage; query: StockMovementListQuery }) {
  const firstItem = page.totalCount === 0 ? 0 : (page.pageNumber - 1) * page.pageSize + 1;
  const lastItem = Math.min(page.pageNumber * page.pageSize, page.totalCount);
  return (
    <footer className="flex flex-col gap-3 border-t border-border bg-surface-subtle/40 px-4 py-3 text-sm text-muted sm:flex-row sm:items-center sm:justify-between sm:px-5">
      <p><span className="font-medium text-foreground">{firstItem}-{lastItem}</span> / {page.totalCount} hareket</p>
      <nav aria-label="Stok hareketleri sayfalama" className="flex flex-wrap items-center gap-2">
        {page.hasPreviousPage ? <Link href={buildStockMovementListHref(query, page.pageNumber - 1)} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 font-medium text-foreground hover:bg-surface-subtle sm:min-h-9">Önceki</Link> : <span aria-disabled="true" className="inline-flex min-h-10 items-center rounded-lg border border-border bg-surface px-3 sm:min-h-9">Önceki</span>}
        <span className="min-w-20 text-center">{page.pageNumber} / {Math.max(page.totalPages, 1)}</span>
        <form action="/inventory/stock-movements" method="get" className="flex items-center gap-1.5">
          {query.pageSize !== 20 ? <input type="hidden" name="pageSize" value={query.pageSize} /> : null}
          {query.search ? <input type="hidden" name="search" value={query.search} /> : null}
          {query.productVariantId ? <input type="hidden" name="productVariantId" value={query.productVariantId} /> : null}
          {query.direction ? <input type="hidden" name="direction" value={query.direction} /> : null}
          {query.type ? <input type="hidden" name="type" value={query.type} /> : null}
          {query.createdFrom ? <input type="hidden" name="createdFrom" value={query.createdFrom} /> : null}
          {query.createdTo ? <input type="hidden" name="createdTo" value={query.createdTo} /> : null}
          {query.balanceVariantId ? <input type="hidden" name="balanceVariantId" value={query.balanceVariantId} /> : null}
          <label className="sr-only" htmlFor="stock-page-number">Sayfa numarasına git</label>
          <input id="stock-page-number" name="pageNumber" type="number" min={1} max={Math.max(page.totalPages, 1)} defaultValue={page.pageNumber} className="min-h-10 w-16 rounded-lg border border-border-strong bg-surface-strong px-2 text-center text-sm font-medium text-foreground outline-none focus:border-primary sm:min-h-9" />
          <button type="submit" className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-2.5 text-xs font-semibold text-foreground hover:bg-surface-subtle sm:min-h-9">Git</button>
        </form>
        {page.hasNextPage ? <Link href={buildStockMovementListHref(query, page.pageNumber + 1)} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 font-medium text-foreground hover:bg-surface-subtle sm:min-h-9">Sonraki</Link> : <span aria-disabled="true" className="inline-flex min-h-10 items-center rounded-lg border border-border bg-surface px-3 sm:min-h-9">Sonraki</span>}
      </nav>
    </footer>
  );
}
