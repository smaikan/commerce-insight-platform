import type { AdminWorkQueueSummaryData } from "@/modules/dashboard/types";

export type WorkQueueKey = "orders" | "contactMessages";

// Burada bilinmeyen BFF gövdesinin güvenli iş kuyruğu sözleşmesine uyup uymadığını doğruluyorum.
export function isAdminWorkQueueSummary(value: unknown): value is AdminWorkQueueSummaryData {
  if (!value || typeof value !== "object") return false;
  const candidate = value as Record<string, unknown>;
  return isCount(candidate.ordersAwaitingProcessingCount)
    && isCount(candidate.newContactMessageCount)
    && typeof candidate.generatedAtUtc === "string"
    && Number.isFinite(Date.parse(candidate.generatedAtUtc));
}

// Burada menü öğesinin bağlı olduğu iş kuyruğu sayacını seçiyorum.
export function getWorkQueueCount(summary: AdminWorkQueueSummaryData | null, key: WorkQueueKey | undefined): number {
  if (!summary || !key) return 0;
  return key === "orders"
    ? summary.ordersAwaitingProcessingCount
    : summary.newContactMessageCount;
}

// Burada geniş sayıların sidebar düzenini bozmaması için görsel rozeti sınırlıyorum.
export function formatWorkQueueCount(count: number): string {
  return count > 99 ? "99+" : String(count);
}

// Burada ekran okuyucuya rozetin iş anlamını sayı ile birlikte açıklıyorum.
export function getWorkQueueAccessibleLabel(label: string, key: WorkQueueKey | undefined, count: number): string | undefined {
  if (!key || count <= 0) return undefined;
  return key === "orders"
    ? `${label}, ${count} işlem bekleyen sipariş`
    : `${label}, ${count} yeni iletişim mesajı`;
}

// Burada sayaç alanlarının negatif olmayan tam sayı olmasını zorunlu tutuyorum.
function isCount(value: unknown): value is number {
  return typeof value === "number" && Number.isSafeInteger(value) && value >= 0;
}
