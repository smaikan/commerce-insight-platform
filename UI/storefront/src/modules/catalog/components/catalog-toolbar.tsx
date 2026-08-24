import Link from "next/link";

import { CATALOG_SORT_LABELS, catalogHref, type CatalogUrlOptions } from "@/modules/catalog/query";
import type { CatalogSort, CatalogView } from "@/modules/catalog/types";

// Burada sıralama seçeneklerini ve ürün adet bilgisini modern, minimalist ve erişilebilir bir lüks toolbar içinde sunuyorum.
export function CatalogToolbar({
  view,
  totalCount,
  urlOptions,
}: {
  view: CatalogView;
  totalCount: number;
  urlOptions?: CatalogUrlOptions;
}) {
  return (
    <div className="flex flex-col gap-4 border-b border-line/70 py-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-center gap-2">
        <span className="size-2 rounded-full bg-brand-700" />
        <p className="text-xs sm:text-sm font-bold tracking-tight text-ink">
          <span className="text-brand-950 font-extrabold">{totalCount}</span> Özel Model Listeleniyor
        </p>
      </div>

      <div className="flex items-center gap-3">
        <span className="text-xs font-bold uppercase tracking-wider text-ink-muted hidden sm:inline-block">
          Sırala:
        </span>
        <nav className="-mx-4 px-4 sm:-mx-0 sm:px-0 flex items-center gap-1.5 overflow-x-auto pb-1 sm:pb-0 scrollbar-none" aria-label="Ürün sıralaması">
          {view.search && !view.hasExplicitSort ? (
            <span aria-current="page" className="shrink-0 rounded-xl bg-brand-950 px-3.5 py-1.5 text-xs font-bold text-white shadow-xs">
              En İlgili
            </span>
          ) : null}
          {(Object.entries(CATALOG_SORT_LABELS) as Array<[CatalogSort, string]>).map(([sort, label]) => {
            const isSelected =
              (view.hasExplicitSort && view.sort === sort) || (!view.search && view.sort === sort);

            return (
              <Link
                key={sort}
                href={catalogHref({ ...view, page: 1, sort, hasExplicitSort: true }, urlOptions)}
                prefetch={false}
                aria-current={isSelected ? "page" : undefined}
                className={`focus-ring shrink-0 rounded-xl px-3 py-1.5 text-xs font-semibold transition-all ${
                  isSelected
                    ? "bg-brand-950 text-white shadow-xs"
                    : "border border-line bg-surface text-ink-muted hover:border-brand-700 hover:text-ink"
                }`}
              >
                {label}
              </Link>
            );
          })}
        </nav>
      </div>
    </div>
  );
}
