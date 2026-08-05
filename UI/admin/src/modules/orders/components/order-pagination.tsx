import Link from "next/link";
import { buildOrderListHref } from "@/modules/orders/query";
import type { OrderListQuery, OrderPage } from "@/modules/orders/types";

// Burada sunucu sayfalamasını mevcut sipariş filtrelerini koruyan önceki ve sonraki bağlantılarıyla sunuyorum.
export function OrderPagination({ page, query }: { page: OrderPage; query: OrderListQuery }) {
  const firstItem = page.totalCount === 0 ? 0 : (page.pageNumber - 1) * page.pageSize + 1;
  const lastItem = Math.min(page.pageNumber * page.pageSize, page.totalCount);

  return (
    <footer className="flex flex-col gap-3 border-t border-border bg-surface-subtle/40 px-4 py-3 text-sm text-muted sm:flex-row sm:items-center sm:justify-between sm:px-5">
      <p><span className="font-medium text-foreground">{firstItem}-{lastItem}</span> / {page.totalCount} sipariş</p>
      <nav aria-label="Sipariş listesi sayfalama" className="flex items-center gap-2">
        {page.hasPreviousPage ? (
          <Link href={buildOrderListHref(query, page.pageNumber - 1)} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 font-medium text-foreground hover:bg-surface-subtle">Önceki</Link>
        ) : (
          <span aria-disabled="true" className="inline-flex min-h-10 items-center rounded-lg border border-border bg-surface px-3 text-muted">Önceki</span>
        )}
        <span className="min-w-20 text-center">{page.pageNumber} / {Math.max(page.totalPages, 1)}</span>
        {page.hasNextPage ? (
          <Link href={buildOrderListHref(query, page.pageNumber + 1)} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 font-medium text-foreground hover:bg-surface-subtle">Sonraki</Link>
        ) : (
          <span aria-disabled="true" className="inline-flex min-h-10 items-center rounded-lg border border-border bg-surface px-3 text-muted">Sonraki</span>
        )}
      </nav>
    </footer>
  );
}
