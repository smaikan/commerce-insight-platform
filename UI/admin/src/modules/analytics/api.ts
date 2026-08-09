import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { AnalyticsDateRange, DashboardProductAnalytics, ProductDailyMetric } from "@/modules/analytics/types";

// Burada dashboard için backend'in tek sorguda hazırladığı dönemsel ürün analitiğini sunucu sınırında getiriyorum.
export function getDashboardProductAnalytics(range: AnalyticsDateRange, session: AdminSession): Promise<DashboardProductAnalytics> {
  const params = new URLSearchParams({ from: range.from, to: range.to });
  return apiRequest(`/api/dashboard/product-analytics?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada tek ürünün günlük ham sayaç serisini aynı doğrulanmış Admin oturumuyla getiriyorum.
export function getProductDailyMetrics(productId: string, range: AnalyticsDateRange, session: AdminSession): Promise<ProductDailyMetric[]> {
  const params = new URLSearchParams({ from: range.from, to: range.to });
  return apiRequest(`/api/product-engagement/products/${encodeURIComponent(productId)}/metrics?${params.toString()}`, {
    accessToken: session.accessToken,
  });
}
