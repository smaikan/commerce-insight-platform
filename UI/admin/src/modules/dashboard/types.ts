import type { components } from "@/generated/api";

// Burada dashboard yanıtını üretilen OpenAPI şemasına doğrudan bağlıyorum.
export type DashboardOverviewData = components["schemas"]["DashboardOverviewDto"];

// Burada admin iş kuyruğu yanıtını üretilen OpenAPI şemasına doğrudan bağlıyorum.
export type AdminWorkQueueSummaryData = components["schemas"]["AdminWorkQueueSummaryDto"];
