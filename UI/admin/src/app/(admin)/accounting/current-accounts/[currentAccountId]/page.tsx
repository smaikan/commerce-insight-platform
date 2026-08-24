import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getCurrentAccount, getCurrentAccountStatement } from "@/modules/accounting/current-accounts/api";
import { CurrentAccountDetail } from "@/modules/accounting/current-accounts/components/current-account-detail";
import { buildCurrentAccountStatementHref, canonicalPageNumber, parseCurrentAccountStatementQuery } from "@/modules/accounting/current-accounts/query";

export const metadata: Metadata = { title: "Cari Hesap Detayı" };

export default async function CurrentAccountDetailPage({ params, searchParams }: { params: Promise<{ currentAccountId: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const [{ currentAccountId }, rawQuery] = await Promise.all([params, searchParams]);
  const session = await requireAdminPageSession(`/accounting/current-accounts/${encodeURIComponent(currentAccountId)}`);
  const statementQuery = parseCurrentAccountStatementQuery(rawQuery);

  let account;
  try {
    account = await getCurrentAccount(currentAccountId, session);
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }

  let statement;
  try {
    statement = await getCurrentAccountStatement(currentAccountId, statementQuery, session);
  } catch (error) {
    if (error instanceof ApiError) return <AccountingLoadProblem problem={error.problem} retryHref={buildCurrentAccountStatementHref(currentAccountId, statementQuery)} />;
    throw error;
  }
  const canonicalStatementPage = canonicalPageNumber(statementQuery.statementPageNumber, statement.totalPages);
  if (canonicalStatementPage) redirect(buildCurrentAccountStatementHref(currentAccountId, statementQuery, canonicalStatementPage));
  const created = rawQuery.created === "1";
  const updated = rawQuery.updated === "1";

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title={account.name}
        description={`Cari kodu: ${account.code}`}
        backHref="/accounting/current-accounts"
        actions={<Link href={`/accounting/current-accounts/${account.id}/edit`} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface px-3.5 text-sm font-semibold hover:bg-surface-subtle">Düzenle</Link>}
      />
      {created || updated ? <p role="status" className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">{created ? "Cari hesap oluşturuldu." : "Cari hesap güncellendi."}</p> : null}
      <CurrentAccountDetail account={account} statement={statement} statementQuery={statementQuery} />
    </div>
  );
}
