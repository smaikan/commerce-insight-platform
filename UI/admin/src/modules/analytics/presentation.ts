import type { ProductDailyMetric } from "@/modules/analytics/types";

// Burada sayı sayaçlarını Türkçe yerel ayarla tutarlı ve kompakt biçimde sunuyorum.
export function formatAnalyticsNumber(value: number): string {
  return value.toLocaleString("tr-TR");
}

// Burada API'nin UTC gün anahtarını yerel gösterim için güvenli bir tarih etiketine çeviriyorum.
export function formatAnalyticsDate(value: string, options: Intl.DateTimeFormatOptions = { day: "numeric", month: "short" }): string {
  const date = new Date(`${value}T00:00:00Z`);
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat("tr-TR", { ...options, timeZone: "UTC" }).format(date);
}

// Burada günlük ham sayaçları yalnızca görünümde dönem toplamı olarak bir araya getiriyorum; iş kuralı veya dönüşüm hesaplamıyorum.
export function summarizeProductDailyMetrics(metrics: ProductDailyMetric[]) {
  return metrics.reduce(
    (total, metric) => ({
      clickCount: total.clickCount + metric.clickCount,
      addToCartCount: total.addToCartCount + metric.addToCartCount,
      purchaseCount: total.purchaseCount + metric.purchaseCount,
      favoriteCount: total.favoriteCount + metric.favoriteCount,
      ratingCount: total.ratingCount + metric.ratingCount,
      reviewCount: total.reviewCount + metric.reviewCount,
    }),
    { clickCount: 0, addToCartCount: 0, purchaseCount: 0, favoriteCount: 0, ratingCount: 0, reviewCount: 0 },
  );
}
