import Link from "next/link";
import { buildProductListHref, productSortOptions } from "@/modules/products/query";
import type { PagedResult, Product, ProductListQuery } from "@/modules/products/types";

// Burada sunucu sayfalamasının toplam ve ileri/geri durumunu filtreleri koruyan bağlantılarla ve doğrudan sayfa atlama alanıyla gösteriyorum.
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
        <form action="/products" method="get" className="flex items-center gap-1.5">
          <input type="hidden" name="pageSize" value={query.pageSize} />
          {query.search ? <input type="hidden" name="search" value={query.search} /> : null}
          {query.typeId ? <input type="hidden" name="typeId" value={query.typeId} /> : null}
          {query.brandId ? <input type="hidden" name="brandId" value={query.brandId} /> : null}
          {query.status !== undefined ? <input type="hidden" name="status" value={query.status} /> : null}
          {query.isFeatured !== undefined ? <input type="hidden" name="isFeatured" value={String(query.isFeatured)} /> : null}
          <input
            type="hidden"
            name="sort"
            value={productSortOptions.find((option) => option.sortBy === query.sortBy && option.descending === query.descending)?.value || productSortOptions[0].value}
          />
          <label className="sr-only" htmlFor="product-page-number">Sayfa numarasına git</label>
          <input
            id="product-page-number"
            name="page"
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
