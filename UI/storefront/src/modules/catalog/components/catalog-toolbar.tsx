import Link from "next/link";

import { CATALOG_SORT_LABELS, catalogHref, type CatalogUrlOptions } from "@/modules/catalog/query";
import type { CatalogSort, CatalogView } from "@/modules/catalog/types";

// Burada belgeli sıralamaları JavaScript gerektirmeyen, paylaşılabilir URL bağlantılarıyla sunuyorum.
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
    <div className="flex flex-col gap-4 border-b border-line py-4 sm:flex-row sm:items-center sm:justify-between">
      <p className="text-sm text-ink-muted">{totalCount} ürün</p>
      <nav className="flex gap-2 overflow-x-auto pb-1 sm:pb-0" aria-label="Ürün sıralaması">
        {(Object.entries(CATALOG_SORT_LABELS) as Array<[CatalogSort, string]>).map(([sort, label]) => (
          <Link
            key={sort}
            href={catalogHref({ ...view, page: 1, sort }, urlOptions)}
            aria-current={view.sort === sort ? "page" : undefined}
            className={`focus-ring shrink-0 rounded-full border px-3 py-2 text-xs font-semibold transition-colors ${
              view.sort === sort
                ? "border-brand-700 bg-brand-700 text-white"
                : "border-line bg-surface text-ink-muted hover:border-brand-600 hover:text-brand-700"
            }`}
          >
            {label}
          </Link>
        ))}
      </nav>
    </div>
  );
}
