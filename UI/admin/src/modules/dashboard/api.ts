import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { DashboardOverviewData } from "@/modules/dashboard/types";

// Burada yöneticiye ait güncel dashboard özetini, tokenı tarayıcıya taşımadan backend'den getiriyorum.
export function getDashboardOverview(session: AdminSession): Promise<DashboardOverviewData> {
  return apiRequest("/api/dashboard/overview", { accessToken: session.accessToken });
}
