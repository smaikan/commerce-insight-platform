import type { components } from "@/generated/api";

// Burada dashboard yanıtını üretilen OpenAPI şemasına doğrudan bağlıyorum.
export type DashboardOverviewData = components["schemas"]["DashboardOverviewDto"];
