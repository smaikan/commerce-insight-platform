import Link from "next/link";

import { catalogHref, type CatalogUrlOptions } from "@/modules/catalog/query";
import type { CatalogView } from "@/modules/catalog/types";

// Burada katalog sayfalarını birbirine bağlı, klavye erişimli önceki ve sonraki bağlantılarıyla sunuyorum.
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
    <nav className="mt-12 flex items-center justify-between border-t border-line pt-6" aria-label="Ürün sayfaları">
      {page > 1 ? (
        <Link className="focus-ring rounded-lg border border-line bg-surface px-4 py-2.5 text-sm font-semibold hover:border-brand-600" href={catalogHref({ ...view, page: page - 1 }, urlOptions)} rel="prev">
          Önceki
        </Link>
      ) : <span />}
      <p className="text-sm text-ink-muted"><span className="font-semibold text-ink">{page}</span> / {totalPages}</p>
      {page < totalPages ? (
        <Link className="focus-ring rounded-lg border border-line bg-surface px-4 py-2.5 text-sm font-semibold hover:border-brand-600" href={catalogHref({ ...view, page: page + 1 }, urlOptions)} rel="next">
          Sonraki
        </Link>
      ) : <span />}
    </nav>
  );
}
