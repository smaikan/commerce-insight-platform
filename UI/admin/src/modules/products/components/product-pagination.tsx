import Link from "next/link";
import { buildProductListHref } from "@/modules/products/query";
import type { PagedResult, Product, ProductListQuery } from "@/modules/products/types";

// Burada sunucu sayfalamasının toplam ve ileri/geri durumunu filtreleri koruyan bağlantılarla gösteriyorum.
export function ProductPagination({ page, query }: { page: PagedResult<Product>; query: ProductListQuery }) {
  const firstItem = page.totalCount === 0 ? 0 : (page.pageNumber - 1) * page.pageSize + 1;
  const lastItem = Math.min(page.pageNumber * page.pageSize, page.totalCount);

  return (
    <footer className="flex flex-col gap-3 border-t border-border bg-surface-subtle/40 px-4 py-3 text-sm text-muted sm:flex-row sm:items-center sm:justify-between sm:px-5">
      <p><span className="font-medium text-foreground">{firstItem}-{lastItem}</span> / {page.totalCount} ürün</p>
      <nav aria-label="Ürün listesi sayfalama" className="flex items-center gap-2">
        {page.hasPreviousPage ? (
          <Link href={buildProductListHref(query, page.pageNumber - 1)} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 font-medium text-foreground hover:bg-surface-subtle">
            Önceki
          </Link>
        ) : (
          <span aria-disabled="true" className="inline-flex min-h-10 items-center rounded-lg border border-border bg-surface px-3 text-muted">Önceki</span>
        )}
        <span className="min-w-20 text-center">{page.pageNumber} / {Math.max(page.totalPages, 1)}</span>
        {page.hasNextPage ? (
          <Link href={buildProductListHref(query, page.pageNumber + 1)} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 font-medium text-foreground hover:bg-surface-subtle">
            Sonraki
          </Link>
        ) : (
          <span aria-disabled="true" className="inline-flex min-h-10 items-center rounded-lg border border-border bg-surface px-3 text-muted">Sonraki</span>
        )}
      </nav>
    </footer>
  );
}
