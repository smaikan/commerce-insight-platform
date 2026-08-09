import Link from "next/link";
import { settingsListHref } from "@/modules/settings/query";
import type { SettingsListQuery } from "@/modules/settings/types";

// Burada kargo ve vergi listelerinde aynı erişilebilir sayfalama düzenini kullanıyorum.
export function SettingsPagination({ basePath, query, totalCount, totalPages }: { basePath: string; query: SettingsListQuery; totalCount: number; totalPages: number }) {
  if (totalPages <= 1) return <p className="border-t border-border px-4 py-3 text-xs text-muted sm:px-5">Toplam {totalCount} kayıt</p>;
  const pageInputId = `${basePath.replace(/[^a-z0-9]+/gi, "-").replace(/^-|-$/g, "")}-page`;
  return (
    <div className="flex flex-col gap-3 border-t border-border px-4 py-3 text-sm sm:flex-row sm:items-center sm:justify-between sm:px-5">
      <p className="text-muted">Toplam {totalCount} kayıt · Sayfa {query.pageNumber}/{totalPages}</p>
      <div className="flex items-center gap-2">
        <PaginationLink disabled={query.pageNumber <= 1} href={settingsListHref(basePath, query, query.pageNumber - 1)}>Önceki</PaginationLink>
        <form action={basePath} method="get" className="flex items-center gap-2">
          <input type="hidden" name="pageSize" value={query.pageSize} />
          <label htmlFor={pageInputId} className="sr-only">Sayfa numarası</label>
          <input id={pageInputId} name="page" type="number" inputMode="numeric" min={1} max={totalPages} defaultValue={query.pageNumber} className="min-h-9 w-16 rounded-lg border border-border-strong bg-surface-strong px-2 text-center text-sm text-foreground" />
          <button className="min-h-9 rounded-lg border border-border-strong bg-surface-strong px-3 font-semibold text-foreground hover:bg-surface-subtle">Git</button>
        </form>
        <PaginationLink disabled={query.pageNumber >= totalPages} href={settingsListHref(basePath, query, query.pageNumber + 1)}>Sonraki</PaginationLink>
      </div>
    </div>
  );
}

// Burada sınır sayfalarında devre dışı durumu link gibi davranmadan gösteriyorum.
function PaginationLink({ disabled, href, children }: { disabled: boolean; href: string; children: React.ReactNode }) {
  return disabled
    ? <span aria-disabled="true" className="inline-flex min-h-9 items-center rounded-lg border border-border px-3 font-semibold text-muted opacity-60">{children}</span>
    : <Link href={href} className="inline-flex min-h-9 items-center rounded-lg border border-border-strong bg-surface-strong px-3 font-semibold text-foreground hover:bg-surface-subtle">{children}</Link>;
}
