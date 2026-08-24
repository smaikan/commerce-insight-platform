import Link from "next/link";

import { catalogHref, type CatalogUrlOptions } from "@/modules/catalog/query";
import type { CatalogView } from "@/modules/catalog/types";
import { PageJumpForm } from "@/modules/catalog/components/page-jump-form";

// Burada katalog sayfalarını lüks numaralandırma hapları, önceki/sonraki bağlantıları ve doğrudan sayfa atlama formuyla sunuyorum.
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

  // Görünür sayfa numaraları (1, 2, 3, vb.)
  const pageNumbers: number[] = [];
  const startPage = Math.max(1, page - 2);
  const endPage = Math.min(totalPages, page + 2);
  for (let i = startPage; i <= endPage; i++) {
    pageNumbers.push(i);
  }

  return (
    <nav
      className="mt-14 flex flex-wrap items-center justify-between gap-4 border-t border-line/70 pt-8"
      aria-label="Ürün sayfaları"
    >
      <div className="flex items-center">
        {page > 1 ? (
          <Link
            className="focus-ring inline-flex items-center gap-2 rounded-xl border border-line bg-surface px-4 py-2.5 text-xs font-bold text-ink hover:border-brand-700 hover:text-brand-950 transition-all shadow-2xs"
            href={catalogHref({ ...view, page: page - 1 }, urlOptions)}
            rel="prev"
          >
            <span aria-hidden="true">&larr;</span>
            <span>Önceki</span>
          </Link>
        ) : (
          <span className="inline-flex items-center gap-2 rounded-xl border border-line/40 bg-surface px-4 py-2.5 text-xs font-semibold text-ink-muted/40 cursor-not-allowed">
            <span aria-hidden="true">&larr;</span>
            <span>Önceki</span>
          </span>
        )}
      </div>

      <div className="flex flex-wrap items-center justify-center gap-2">
        {startPage > 1 ? (
          <>
            <Link
              href={catalogHref({ ...view, page: 1 }, urlOptions)}
              prefetch={false}
              className="focus-ring flex size-9 items-center justify-center rounded-xl border border-line bg-surface text-xs font-semibold text-ink hover:border-brand-700 transition-colors"
            >
              1
            </Link>
            {startPage > 2 ? <span className="px-1 text-xs text-ink-muted">&bull;&bull;&bull;</span> : null}
          </>
        ) : null}

        {pageNumbers.map((p) => {
          const isCurrent = p === page;
          return isCurrent ? (
            <span
              key={p}
              aria-current="page"
              className="flex size-9 items-center justify-center rounded-xl bg-brand-950 text-xs font-bold text-white shadow-xs"
            >
              {p}
            </span>
          ) : (
            <Link
              key={p}
              href={catalogHref({ ...view, page: p }, urlOptions)}
              prefetch={false}
              className="focus-ring flex size-9 items-center justify-center rounded-xl border border-line bg-surface text-xs font-semibold text-ink hover:border-brand-700 transition-colors"
            >
              {p}
            </Link>
          );
        })}

        {endPage < totalPages ? (
          <>
            {endPage < totalPages - 1 ? <span className="px-1 text-xs text-ink-muted">&bull;&bull;&bull;</span> : null}
            <Link
              href={catalogHref({ ...view, page: totalPages }, urlOptions)}
              prefetch={false}
              className="focus-ring flex size-9 items-center justify-center rounded-xl border border-line bg-surface text-xs font-semibold text-ink hover:border-brand-700 transition-colors"
            >
              {totalPages}
            </Link>
          </>
        ) : null}

        <div className="ml-3 hidden sm:flex items-center gap-2 border-l border-line/70 pl-3">
          <PageJumpForm
            currentPage={page}
            totalPages={totalPages}
            hrefTemplate={catalogHref({ ...view, page: 999999 }, urlOptions).replace("999999", "__PAGE__")}
          />
        </div>
      </div>

      <div className="flex items-center">
        {page < totalPages ? (
          <Link
            className="focus-ring inline-flex items-center gap-2 rounded-xl border border-line bg-surface px-4 py-2.5 text-xs font-bold text-ink hover:border-brand-700 hover:text-brand-950 transition-all shadow-2xs"
            href={catalogHref({ ...view, page: page + 1 }, urlOptions)}
            rel="next"
          >
            <span>Sonraki</span>
            <span aria-hidden="true">&rarr;</span>
          </Link>
        ) : (
          <span className="inline-flex items-center gap-2 rounded-xl border border-line/40 bg-surface px-4 py-2.5 text-xs font-semibold text-ink-muted/40 cursor-not-allowed">
            <span>Sonraki</span>
            <span aria-hidden="true">&rarr;</span>
          </span>
        )}
      </div>
    </nav>
  );
}
