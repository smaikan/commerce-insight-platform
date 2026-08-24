import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getCurrentAccount } from "@/modules/accounting/current-accounts/api";
import { CurrentAccountForm } from "@/modules/accounting/current-accounts/components/current-account-form";

export const metadata: Metadata = { title: "Cari Hesap Düzenle" };

export default async function EditCurrentAccountPage({ params }: { params: Promise<{ currentAccountId: string }> }) {
  const { currentAccountId } = await params;
  const session = await requireAdminPageSession(`/accounting/current-accounts/${encodeURIComponent(currentAccountId)}/edit`);
  let account;
  try {
    account = await getCurrentAccount(currentAccountId, session);
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    if (error instanceof ApiError) return <AccountingLoadProblem problem={error.problem} retryHref={`/accounting/current-accounts/${encodeURIComponent(currentAccountId)}/edit`} />;
    throw error;
  }
  return (
    <div className="mx-auto w-full max-w-6xl">
      <PageHeader title="Cari hesabı düzenle" description={account.name} backHref={`/accounting/current-accounts/${account.id}`} backLabel="Cari hesaba dön" />
      <CurrentAccountForm account={account} />
    </div>
  );
}
