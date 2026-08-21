import Link from "next/link";

import { catalogHref, type CatalogUrlOptions } from "@/modules/catalog/query";
import type { CatalogView } from "@/modules/catalog/types";
import { PageJumpForm } from "@/modules/catalog/components/page-jump-form";

// Burada katalog sayfalarını önceki, sonraki ve doğrudan sayfa numarası giriş formuyla sunuyorum.
export function CatalogPagination({
  page,
  totalPages,
  view,
  urlOptions,
}: {
  page: number;
  totalPages: number;
  view: CatalogView;
  urlOptions?: CatalogUrlOptions;
}) {
  if (totalPages <= 1) return null;

  return (
    <nav className="mt-12 flex flex-wrap items-center justify-between gap-4 border-t border-line pt-6" aria-label="Ürün sayfaları">
      <div className="flex items-center">
        {page > 1 ? (
          <Link className="focus-ring inline-flex items-center rounded-lg border border-line bg-surface px-4 py-2.5 text-sm font-semibold text-ink hover:border-brand-600 transition-colors" href={catalogHref({ ...view, page: page - 1 }, urlOptions)} rel="prev">
            Önceki
          </Link>
        ) : <span />}
      </div>
      
      <div className="flex flex-wrap items-center justify-center gap-3 sm:gap-4">
        <p className="text-sm text-ink-muted">
          Sayfa <span className="font-semibold text-ink">{page}</span> / <span className="font-semibold text-ink">{totalPages}</span>
        </p>
        <PageJumpForm
          currentPage={page}
          totalPages={totalPages}
          hrefTemplate={catalogHref({ ...view, page: 999999 }, urlOptions).replace("999999", "__PAGE__")}
        />
      </div>

      <div className="flex items-center">
        {page < totalPages ? (
          <Link className="focus-ring inline-flex items-center rounded-lg border border-line bg-surface px-4 py-2.5 text-sm font-semibold text-ink hover:border-brand-600 transition-colors" href={catalogHref({ ...view, page: page + 1 }, urlOptions)} rel="next">
            Sonraki
          </Link>
        ) : <span />}
      </div>
    </nav>
  );
}
