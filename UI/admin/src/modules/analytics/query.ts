import type { AnalyticsDateRange, AnalyticsPeriod } from "@/modules/analytics/types";

const analyticsPeriods = [7, 30, 90] as const satisfies readonly AnalyticsPeriod[];

type SearchParams = Record<string, string | string[] | undefined>;

// Burada paylaşılabilir URL parametresini yalnızca desteklenen analiz dönemlerinden birine çeviriyorum.
export function parseAnalyticsPeriod(searchParams: SearchParams): AnalyticsPeriod {
  const value = first(searchParams.analyticsPeriod);
  const parsed = Number(value);
  return analyticsPeriods.includes(parsed as AnalyticsPeriod) ? (parsed as AnalyticsPeriod) : 30;
}

// Burada UTC gün sınırına göre, iki ucu da içeren ve sözleşmedeki 90 gün sınırını aşmayan tarih aralığını üretiyorum.
export function getAnalyticsDateRange(period: AnalyticsPeriod, now = new Date()): AnalyticsDateRange {
  const to = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  const from = new Date(to);
  from.setUTCDate(from.getUTCDate() - period + 1);

  return {
    period,
    from: toIsoDate(from),
    to: toIsoDate(to),
  };
}

// Burada dönem değiştirirken mevcut sayfanın diğer URL bağlamını koruyan güvenli bir bağlantı oluşturuyorum.
export function buildAnalyticsPeriodHref(pathname: string, searchParams: SearchParams, period: AnalyticsPeriod): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    const item = first(value);
    if (item && key !== "analyticsPeriod") params.set(key, item);
  }
  params.set("analyticsPeriod", String(period));
  return `${pathname}?${params.toString()}`;
}

// Burada çok değerli URL parametrelerinden yalnızca ilk değeri kullanıyorum.
function first(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada API'nin beklediği tarih telini saat dilimi kayması olmadan biçimliyorum.
function toIsoDate(value: Date): string {
  return value.toISOString().slice(0, 10);
}
