import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { getDashboardProductAnalytics } from "@/modules/analytics/api";
import { AnalyticsUnavailable, DashboardProductAnalyticsPanel } from "@/modules/analytics/components/analytics-panels";
import { getAnalyticsDateRange, parseAnalyticsPeriod } from "@/modules/analytics/query";
import { getDashboardOverview } from "@/modules/dashboard/api";
import { DashboardOverview } from "@/modules/dashboard/components/dashboard-overview";

export const metadata: Metadata = {
  title: "Genel Bakış",
};

// Burada dashboard verilerini doğrulanmış Admin oturumuyla ve seçili UTC dönemine göre paralel olarak getiriyorum.
export default async function DashboardPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const query = await searchParams;
  const selectedPeriod = parseAnalyticsPeriod(query);
  const range = getAnalyticsDateRange(selectedPeriod);
  const session = await requireAdminPageSession("/dashboard");
  const [overview, analytics] = await Promise.all([
    getDashboardOverview(session),
    getDashboardProductAnalytics(range, session).catch(() => null),
  ]);

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <DashboardOverview overview={overview} />
      {analytics ? (
        <DashboardProductAnalyticsPanel analytics={analytics} selectedPeriod={selectedPeriod} searchParams={query} />
      ) : (
        <AnalyticsUnavailable title="Ürün analizi yüklenemedi" />
      )}
    </div>
  );
}
