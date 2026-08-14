import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { settingsListHref } from "@/modules/settings/query";
import type { SettingsListQuery } from "@/modules/settings/types";

// Burada kargo ve vergi listelerini ortak admin sayfalama düzenine bağlayıp sayfa boyutunu koruyorum.
export function SettingsPagination({
  basePath,
  query,
  totalCount,
  totalPages,
}: {
  basePath: string;
  query: SettingsListQuery;
  totalCount: number;
  totalPages: number;
}) {
  return (
    <AdminPagination
      action={basePath}
      ariaLabel="Ayar listesi sayfalama"
      buildHref={(pageNumber) => settingsListHref(basePath, query, pageNumber)}
      hiddenFields={[{ name: "pageSize", value: query.pageSize }]}
      itemLabel="kayıt"
      pageNumber={query.pageNumber}
      pageParam="page"
      pageSize={query.pageSize}
      totalCount={totalCount}
      totalPages={totalPages}
    />
  );
}
