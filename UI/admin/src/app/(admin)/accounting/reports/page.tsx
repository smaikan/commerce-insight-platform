import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { ReportCatalog } from "@/modules/accounting/reports/components/report-catalog";

export const metadata: Metadata = { title: "Muhasebe Raporları" };

// Burada rapor dizinini yönetici oturumu arkasında, muhasebe modülünün ayrı çalışma alanı olarak açıyorum.
export default async function AccountingReportsPage() {
  await requireAdminPageSession("/accounting/reports");
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Muhasebe Raporları" description="Belge, FIFO, kârlılık, cari, nakit ve KDV raporlarını kendi finansal sözlükleriyle inceleyin." backHref="/accounting" /><ReportCatalog /></div>;
}
