import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getCurrentAccounts } from "@/modules/accounting/current-accounts/api";
import { CurrentAccountPagination } from "@/modules/accounting/current-accounts/components/current-account-pagination";
import { CurrentAccountTable } from "@/modules/accounting/current-accounts/components/current-account-table";
import { buildCurrentAccountListHref, canonicalPageNumber, parseCurrentAccountListQuery } from "@/modules/accounting/current-accounts/query";

export const metadata: Metadata = { title: "Cari Hesaplar" };

export default async function CurrentAccountsPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const params = await searchParams;
  const query = parseCurrentAccountListQuery(params);
  const session = await requireAdminPageSession(buildCurrentAccountListHref(query));
  let page;
  try {
    page = await getCurrentAccounts(query, session);
  } catch (error) {
    if (error instanceof ApiError) return <AccountingLoadProblem problem={error.problem} retryHref={buildCurrentAccountListHref(query)} />;
    throw error;
  }
  const canonicalPage = canonicalPageNumber(query.pageNumber, page.totalPages);
  if (canonicalPage) redirect(buildCurrentAccountListHref(query, canonicalPage));

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title="Cari Hesaplar"
        description="Müşteri ve tedarikçi muhasebe master kayıtlarını yönetin; borç ve alacak hareketlerini ekstrede izleyin."
        backHref="/accounting"
        actions={<Link href="/accounting/current-accounts/new" className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover">Cari hesap oluştur</Link>}
      />
      <p className="mb-4 rounded-lg border border-border bg-surface-subtle/60 px-3 py-2 text-xs text-muted">Bu liste API sözleşmesi gereği yalnız sayfalanabilir; arama ve tür filtresi desteklenmiyor.</p>
      {page.items.length ? <p className="mb-2 text-xs font-medium text-muted sm:hidden">Tüm kolonlar için tabloyu yatay kaydırın.</p> : null}
      <section aria-label="Cari hesap listesi" className="overflow-hidden rounded-xl border border-border bg-surface">
        <CurrentAccountTable page={page} />
        <CurrentAccountPagination page={page} query={query} />
      </section>
    </div>
  );
}
