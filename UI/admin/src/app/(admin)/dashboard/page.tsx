import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { DashboardOverview } from "@/modules/dashboard/components/dashboard-overview";

export const metadata: Metadata = {
  title: "Genel Bakış",
};

// Burada kısmi layout render davranışına güvenmeden dashboard girişinde Admin rolünü yeniden doğruluyorum.
export default async function DashboardPage() {
  await requireAdminPageSession("/dashboard");
  return <DashboardOverview />;
}
