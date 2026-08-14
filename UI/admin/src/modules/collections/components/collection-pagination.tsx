import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { buildCollectionListHref } from "@/modules/collections/query";
import type { CollectionListQuery, CollectionPage } from "@/modules/collections/types";

// Burada koleksiyon listesini ortak admin sayfalama düzenine bağlayıp seçili sayfa boyutunu doğrudan atlamada koruyorum.
export function CollectionPagination({ page, query }: { page: CollectionPage; query: CollectionListQuery }) {
  return (
    <AdminPagination
      action="/collections"
      ariaLabel="Koleksiyon listesi sayfalama"
      buildHref={(pageNumber) => buildCollectionListHref(query, pageNumber)}
      hiddenFields={query.pageSize !== 20 ? [{ name: "pageSize", value: query.pageSize }] : []}
      itemLabel="koleksiyon"
      pageNumber={page.pageNumber}
      pageSize={page.pageSize}
      totalCount={page.totalCount}
      totalPages={page.totalPages}
    />
  );
}
