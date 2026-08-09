import type { components } from "@/generated/api";

// Burada ürün analitiği yanıtlarını üretilen OpenAPI sözleşmesinden doğrudan türetiyorum.
export type DashboardProductAnalytics = components["schemas"]["DashboardProductAnalyticsDto"];
export type ProductDailyMetric = components["schemas"]["ProductMetricDto"];

// Burada URL'de izin verilen sabit dönem seçeneklerini tek kaynaktan tanımlıyorum.
export type AnalyticsPeriod = 7 | 30 | 90;

export type AnalyticsDateRange = {
  period: AnalyticsPeriod;
  from: string;
  to: string;
};
