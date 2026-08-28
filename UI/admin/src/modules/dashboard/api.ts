import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { AdminWorkQueueSummaryData, DashboardOverviewData } from "@/modules/dashboard/types";

// Burada yöneticiye ait güncel dashboard özetini, tokenı tarayıcıya taşımadan backend'den getiriyorum.
export function getDashboardOverview(session: AdminSession): Promise<DashboardOverviewData> {
  return apiRequest("/api/dashboard/overview", { accessToken: session.accessToken });
}

// Burada admin iş kuyruğu sayaçlarını tokenı tarayıcıya taşımadan backend'den getiriyorum.
export function getAdminWorkQueueSummary(session: AdminSession): Promise<AdminWorkQueueSummaryData> {
  return apiRequest("/api/dashboard/work-queue-summary", { accessToken: session.accessToken });
}
