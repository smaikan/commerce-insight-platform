import type { Metadata } from "next";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getAccountingQueues } from "@/modules/accounting/core/api";
import { AccountingOverview } from "@/modules/accounting/core/components/accounting-overview";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";

export const metadata: Metadata = { title: "Ön Muhasebe" };

export default async function AccountingPage() {
  const session = await requireAdminPageSession("/accounting");
  let queues;
  try {
    queues = await getAccountingQueues(session);
  } catch (error) {
    if (error instanceof ApiError) return <AccountingLoadProblem problem={error.problem} retryHref="/accounting" />;
    throw error;
  }
  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader title="Ön Muhasebe" description="Cari, belge, tahsilat ve maliyet süreçlerini ayrı muhasebe kayıtları üzerinden yönetin." />
      <AccountingOverview queues={queues} />
    </div>
  );
}
