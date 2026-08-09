import Link from "next/link";
import { formatAnalyticsDate, formatAnalyticsNumber, summarizeProductDailyMetrics } from "@/modules/analytics/presentation";
import { buildAnalyticsPeriodHref } from "@/modules/analytics/query";
import type { AnalyticsPeriod, DashboardProductAnalytics, ProductDailyMetric } from "@/modules/analytics/types";

type SearchParams = Record<string, string | string[] | undefined>;

const periods: ReadonlyArray<{ value: AnalyticsPeriod; label: string }> = [
  { value: 7, label: "7 gün" },
  { value: 30, label: "30 gün" },
  { value: 90, label: "90 gün" },
];

type MetricTotals = Pick<DashboardProductAnalytics, "clickCount" | "addToCartCount" | "purchaseCount" | "favoriteCount" | "ratingCount" | "reviewCount">;

// Burada URL tabanlı dönem seçicisini istemci durumu oluşturmadan, paylaşılabilir bağlantılarla sunuyorum.
export function AnalyticsPeriodSelector({ pathname, searchParams, selectedPeriod }: { pathname: string; searchParams: SearchParams; selectedPeriod: AnalyticsPeriod }) {
  return (
    <nav aria-label="Analiz dönemi" className="inline-flex rounded-lg border border-border bg-surface-subtle p-0.5">
      {periods.map((period) => {
        const isSelected = selectedPeriod === period.value;
        return (
          <Link
            key={period.value}
            href={buildAnalyticsPeriodHref(pathname, searchParams, period.value)}
            aria-current={isSelected ? "page" : undefined}
            className={`inline-flex min-h-8 items-center rounded-md px-2.5 text-xs font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus ${isSelected ? "bg-surface text-foreground shadow-sm" : "text-muted hover:text-foreground"}`}
          >
            Son {period.label}
          </Link>
        );
      })}
    </nav>
  );
}

// Burada dashboard'un backend tarafından hazırlanmış dönem toplamı, günlük seri ve ürün sıralamasını tek çalışma yüzeyinde birleştiriyorum.
export function DashboardProductAnalyticsPanel({ analytics, selectedPeriod, searchParams }: { analytics: DashboardProductAnalytics; selectedPeriod: AnalyticsPeriod; searchParams: SearchParams }) {
  return (
    <section aria-labelledby="product-analytics-title" className="mt-5 overflow-hidden rounded-xl border border-border bg-surface">
      <div className="flex flex-col gap-3 border-b border-border px-4 py-4 sm:flex-row sm:items-start sm:justify-between sm:px-5">
        <div>
          <h2 id="product-analytics-title" className="text-base font-semibold text-foreground">Ürün analizi</h2>
          <p className="mt-1 text-sm text-muted">{formatAnalyticsDate(analytics.from, { day: "numeric", month: "long" })} – {formatAnalyticsDate(analytics.to, { day: "numeric", month: "long", year: "numeric" })} arasındaki ürün etkileşimleri.</p>
        </div>
        <AnalyticsPeriodSelector pathname="/dashboard" searchParams={searchParams} selectedPeriod={selectedPeriod} />
      </div>

      <div className="grid divide-y divide-border lg:grid-cols-[minmax(0,1fr)_22rem] lg:divide-x lg:divide-y-0">
        <div className="p-4 sm:p-5">
          <AnalyticsMetrics totals={analytics} />
          <AnalyticsSeriesChart metrics={analytics.dailyMetrics} title="Günlük ürün etkileşimleri" />
        </div>
        <TopProducts products={analytics.topProducts} />
      </div>
      <p className="border-t border-border bg-surface-subtle px-4 py-2 text-xs text-muted sm:px-5">Veri zamanı: {new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short", timeZone: "Europe/Istanbul" }).format(new Date(analytics.generatedAtUtc))}</p>
    </section>
  );
}

// Burada ürün düzenleme ekranındaki günlük seriyi, dönem seçimi ve ham sayaçların okunabilir özetiyle sunuyorum.
export function ProductAnalyticsPanel({ metrics, selectedPeriod, searchParams, productId }: { metrics: ProductDailyMetric[]; selectedPeriod: AnalyticsPeriod; searchParams: SearchParams; productId: string }) {
  const totals = summarizeProductDailyMetrics(metrics);
  const hasActivity = Object.values(totals).some((value) => value > 0);

  return (
    <section aria-labelledby="product-performance-title" className="mb-5 overflow-hidden rounded-xl border border-border bg-surface">
      <div className="flex flex-col gap-3 border-b border-border px-4 py-4 sm:flex-row sm:items-start sm:justify-between sm:px-5">
        <div>
          <h2 id="product-performance-title" className="text-base font-semibold text-foreground">Ürün performansı</h2>
          <p className="mt-1 text-sm text-muted">Görüntülenme, sepete ekleme ve satın alma hareketleri UTC günlerine göre izlenir.</p>
        </div>
        <AnalyticsPeriodSelector pathname={`/products/${encodeURIComponent(productId)}`} searchParams={searchParams} selectedPeriod={selectedPeriod} />
      </div>
      <div className="p-4 sm:p-5">
        <AnalyticsMetrics totals={totals} />
        {hasActivity ? <AnalyticsSeriesChart metrics={metrics} title="Günlük ürün performansı" /> : <AnalyticsEmptyState />}
      </div>
    </section>
  );
}

// Burada metrik servisi geçici olarak erişilemediğinde ürün düzenleme akışını kesmeden anlamlı bir ara durum gösteriyorum.
export function AnalyticsUnavailable({ title = "Ürün performansı yüklenemedi" }: { title?: string }) {
  return (
    <section className="mb-5 rounded-xl border border-warning/35 bg-surface px-4 py-4 sm:px-5" role="status">
      <h2 className="text-sm font-semibold text-foreground">{title}</h2>
      <p className="mt-1 text-sm leading-5 text-muted">Diğer kayıt bilgileri kullanılabilir. Sayfayı yenileyerek analizi tekrar deneyebilirsiniz.</p>
    </section>
  );
}

// Burada sadece API'den gelen ham sayaçları karar verme sırasına göre sıkı metrik gruplarında gösteriyorum.
function AnalyticsMetrics({ totals }: { totals: MetricTotals }) {
  const primary = [
    { label: "Görüntülenme", value: totals.clickCount },
    { label: "Sepete ekleme", value: totals.addToCartCount },
    { label: "Satın alma", value: totals.purchaseCount },
  ];
  const secondary = [
    { label: "Favori", value: totals.favoriteCount },
    { label: "Puanlama", value: totals.ratingCount },
    { label: "Yorum", value: totals.reviewCount },
  ];

  return (
    <div className="grid gap-2 sm:grid-cols-3">
      {primary.map((metric) => (
        <div key={metric.label} className="rounded-lg border border-border bg-surface-strong px-3 py-3">
          <p className="text-xs font-medium text-muted">{metric.label}</p>
          <p className="mt-1 font-mono text-xl font-semibold tracking-tight text-foreground tabular-nums">{formatAnalyticsNumber(metric.value)}</p>
        </div>
      ))}
      <div className="sm:col-span-3">
        <dl className="flex flex-wrap gap-x-4 gap-y-1 pt-1 text-xs text-muted">
          {secondary.map((metric) => <div key={metric.label} className="flex items-baseline gap-1"><dt>{metric.label}</dt><dd className="font-mono font-semibold text-foreground tabular-nums">{formatAnalyticsNumber(metric.value)}</dd></div>)}
        </dl>
      </div>
    </div>
  );
}

// Burada üç ana davranışı bağımlılıksız SVG çizgileriyle karşılaştırıyor, ekran okuyucu için günlük tablodan aynı veriye erişim sağlıyorum.
function AnalyticsSeriesChart({ metrics, title }: { metrics: ProductDailyMetric[]; title: string }) {
  const maximum = Math.max(1, ...metrics.flatMap((metric) => [metric.clickCount, metric.addToCartCount, metric.purchaseCount]));
  const width = 720;
  const height = 196;
  const padding = { top: 14, right: 12, bottom: 26, left: 38 };
  const plotWidth = width - padding.left - padding.right;
  const plotHeight = height - padding.top - padding.bottom;
  const x = (index: number) => padding.left + (metrics.length <= 1 ? plotWidth / 2 : (index / (metrics.length - 1)) * plotWidth);
  const y = (value: number) => padding.top + plotHeight - (value / maximum) * plotHeight;
  const path = (key: "clickCount" | "addToCartCount" | "purchaseCount") => metrics.map((metric, index) => `${index === 0 ? "M" : "L"}${x(index).toFixed(2)} ${y(metric[key]).toFixed(2)}`).join(" ");
  const labelIndexes = Array.from(new Set([0, Math.floor((metrics.length - 1) / 2), Math.max(0, metrics.length - 1)]));

  return (
    <div className="mt-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-semibold text-foreground">{title}</h3>
        <div className="flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted" aria-label="Grafik açıklaması">
          <span><i aria-hidden="true" className="mr-1 inline-block size-2 rounded-full bg-primary" />Görüntülenme</span>
          <span><i aria-hidden="true" className="mr-1 inline-block size-2 rounded-full bg-sky-500" />Sepete ekleme</span>
          <span><i aria-hidden="true" className="mr-1 inline-block size-2 rounded-full bg-emerald-600" />Satın alma</span>
        </div>
      </div>
      <div className="mt-3 overflow-x-auto rounded-lg border border-border bg-surface-subtle p-2 sm:p-3">
        <svg role="img" aria-labelledby="analytics-chart-title analytics-chart-description" viewBox={`0 0 ${width} ${height}`} className="block min-w-[34rem] w-full" preserveAspectRatio="none">
          <title id="analytics-chart-title">{title}</title>
          <desc id="analytics-chart-description">Dikey eksen en yüksek günlük sayaç değerine, yatay eksen seçili dönem içindeki UTC günlerine göre ölçeklenir.</desc>
          {[0, 0.5, 1].map((step) => <line key={step} x1={padding.left} x2={width - padding.right} y1={padding.top + plotHeight * step} y2={padding.top + plotHeight * step} className="stroke-border" strokeWidth="1" />)}
          <text x={padding.left - 5} y={padding.top + 4} textAnchor="end" className="fill-muted text-[10px]">{formatAnalyticsNumber(maximum)}</text>
          <text x={padding.left - 5} y={padding.top + plotHeight + 4} textAnchor="end" className="fill-muted text-[10px]">0</text>
          {labelIndexes.map((index) => <text key={index} x={x(index)} y={height - 7} textAnchor={index === 0 ? "start" : index === metrics.length - 1 ? "end" : "middle"} className="fill-muted text-[10px]">{metrics[index] ? formatAnalyticsDate(metrics[index].date) : ""}</text>)}
          <path d={path("clickCount")} fill="none" className="stroke-primary" strokeWidth="2.5" vectorEffect="non-scaling-stroke" />
          <path d={path("addToCartCount")} fill="none" className="stroke-sky-500" strokeWidth="2.5" vectorEffect="non-scaling-stroke" />
          <path d={path("purchaseCount")} fill="none" className="stroke-emerald-600" strokeWidth="2.5" vectorEffect="non-scaling-stroke" />
        </svg>
      </div>
      <details className="mt-3 rounded-lg border border-border bg-surface-strong">
        <summary className="cursor-pointer px-3 py-2 text-xs font-semibold text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-focus">Günlük veriyi tablo olarak gör</summary>
        <div className="overflow-x-auto border-t border-border">
          <table className="min-w-full text-left text-xs">
            <thead className="bg-surface-subtle text-muted"><tr><th scope="col" className="px-3 py-2 font-medium">Tarih</th><th scope="col" className="px-3 py-2 text-right font-medium">Görüntülenme</th><th scope="col" className="px-3 py-2 text-right font-medium">Sepete ekleme</th><th scope="col" className="px-3 py-2 text-right font-medium">Satın alma</th></tr></thead>
            <tbody className="divide-y divide-border">{metrics.map((metric) => <tr key={metric.date}><th scope="row" className="whitespace-nowrap px-3 py-2 font-medium text-foreground">{formatAnalyticsDate(metric.date, { day: "numeric", month: "long", year: "numeric" })}</th><td className="px-3 py-2 text-right font-mono tabular-nums text-foreground">{formatAnalyticsNumber(metric.clickCount)}</td><td className="px-3 py-2 text-right font-mono tabular-nums text-foreground">{formatAnalyticsNumber(metric.addToCartCount)}</td><td className="px-3 py-2 text-right font-mono tabular-nums text-foreground">{formatAnalyticsNumber(metric.purchaseCount)}</td></tr>)}</tbody>
          </table>
        </div>
      </details>
    </div>
  );
}

// Burada sıralama semantiğini backend'in satın alma, sepete ekleme ve görüntülenme önceliğiyle aynen koruyorum.
function TopProducts({ products }: { products: DashboardProductAnalytics["topProducts"] }) {
  return (
    <section aria-labelledby="top-products-title" className="min-w-0">
      <div className="border-b border-border px-4 py-3 sm:px-5"><h3 id="top-products-title" className="text-sm font-semibold text-foreground">Öne çıkan ürünler</h3><p className="mt-1 text-xs text-muted">Satın alma, sepete ekleme ve görüntülenme sırasıyla.</p></div>
      {products.length ? <ol className="divide-y divide-border">{products.map((product, index) => <li key={product.productId} className="flex gap-3 px-4 py-3 sm:px-5"><span aria-label={`${index + 1}. sıra`} className="flex size-6 shrink-0 items-center justify-center rounded-md bg-surface-subtle font-mono text-xs font-semibold text-muted">{index + 1}</span><div className="min-w-0 flex-1"><Link href={`/products/${encodeURIComponent(product.productId)}`} className="block truncate text-sm font-semibold text-foreground hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">{product.title}</Link><p className="mt-0.5 truncate font-mono text-xs text-muted">{product.mainSku}</p><p className="mt-2 text-xs text-muted"><span className="font-mono font-semibold text-foreground tabular-nums">{formatAnalyticsNumber(product.purchaseCount)}</span> satın alma <span aria-hidden="true">·</span> <span className="font-mono font-semibold text-foreground tabular-nums">{formatAnalyticsNumber(product.addToCartCount)}</span> sepete ekleme <span aria-hidden="true">·</span> <span className="font-mono font-semibold text-foreground tabular-nums">{formatAnalyticsNumber(product.clickCount)}</span> görüntülenme</p></div></li>)}</ol> : <p className="px-4 py-6 text-sm leading-6 text-muted sm:px-5">Bu dönemde ürün hareketi bulunmuyor.</p>}
    </section>
  );
}

// Burada sıfır dolu seri geldiğinde grafik yerine veri yok durumunu açık ve kısa biçimde açıklıyorum.
function AnalyticsEmptyState() {
  return <p className="mt-5 rounded-lg border border-dashed border-border bg-surface-subtle px-4 py-5 text-sm leading-6 text-muted">Seçili dönemde bu ürün için kayıtlı etkileşim bulunmuyor.</p>;
}
