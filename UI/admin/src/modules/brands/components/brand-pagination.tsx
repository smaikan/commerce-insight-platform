import Link from "next/link";
import { buildBrandListHref } from "@/modules/brands/query";
import type { BrandListQuery, BrandPage } from "@/modules/brands/types";

// Burada önceki/sonraki bağlantılarını ve doğrudan sayfa girişini aynı erişilebilir alanda sunuyorum.
export function BrandPagination({ page, query }: { page: BrandPage; query: BrandListQuery }) {
  const firstItem = page.totalCount === 0 ? 0 : (page.pageNumber - 1) * page.pageSize + 1;
  const lastItem = Math.min(page.pageNumber * page.pageSize, page.totalCount);
  const totalPages = Math.max(page.totalPages, 1);

  return (
    <footer className="flex flex-col gap-3 border-t border-border bg-surface-subtle/40 px-4 py-3 text-sm text-muted sm:flex-row sm:items-center sm:justify-between">
      <p><span className="font-medium text-foreground">{firstItem}-{lastItem}</span> / {page.totalCount} marka</p>
      <nav aria-label="Marka sayfalama" className="flex flex-wrap items-center gap-2">
        <PageLink disabled={!page.hasPreviousPage} href={buildBrandListHref(query, page.pageNumber - 1)}>Önceki</PageLink>
        <span className="min-w-20 text-center">{page.pageNumber} / {totalPages}</span>
        <form action="/brands" method="get" className="flex items-center gap-1.5">
          {query.pageSize !== 20 ? <input type="hidden" name="pageSize" value={query.pageSize} /> : null}
          <label className="sr-only" htmlFor="brand-page-number">Sayfa numarasına git</label>
          <input id="brand-page-number" name="pageNumber" type="number" inputMode="numeric" min={1} max={totalPages} defaultValue={page.pageNumber} className="min-h-10 w-16 rounded-lg border border-border-strong bg-surface-strong px-2 text-center text-sm font-medium text-foreground outline-none focus:border-primary sm:min-h-9" />
          <button type="submit" className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-2.5 text-xs font-semibold text-foreground hover:bg-surface-subtle sm:min-h-9">Git</button>
        </form>
        <PageLink disabled={!page.hasNextPage} href={buildBrandListHref(query, page.pageNumber + 1)}>Sonraki</PageLink>
      </nav>
    </footer>
  );
}

// Burada kullanılamayan sayfalama eylemlerini bağlantı oluşturmadan okunabilir tutuyorum.
function PageLink({ disabled, href, children }: { disabled: boolean; href: string; children: React.ReactNode }) {
  const className = "inline-flex min-h-10 items-center rounded-lg border px-3 text-sm font-medium sm:min-h-9";
  return disabled
    ? <span aria-disabled="true" className={`${className} border-border bg-surface text-muted`}>{children}</span>
    : <Link href={href} className={`${className} border-border-strong bg-surface-strong text-foreground hover:bg-surface-subtle`}>{children}</Link>;
}
