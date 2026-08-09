import Link from "next/link";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { formatDashboardCurrency, formatDashboardGeneratedAt } from "@/modules/dashboard/presentation";
import type { DashboardOverviewData } from "@/modules/dashboard/types";

const quickLinks = [
  { name: "Ürünler", description: "Katalog ve stok durumunu yönetin.", href: "/products", icon: "box" },
  { name: "Siparişler", description: "Siparişleri ve ödeme durumlarını takip edin.", href: "/orders", icon: "order" },
  { name: "Müşteriler", description: "Müşteri hesaplarını ve sipariş geçmişini görüntüleyin.", href: "/customers", icon: "users" },
  { name: "Koleksiyonlar", description: "Manuel ürün gruplarını düzenleyin.", href: "/collections", icon: "layers" },
] as const;

// Burada hızlı erişim satırlarının ikonunu tek bir yalın SVG bileşeninde eşliyorum.
function QuickLinkIcon({ name }: { name: (typeof quickLinks)[number]["icon"] }) {
  const path =
    name === "box"
      ? "M4 7.5 12 3l8 4.5v9L12 21l-8-4.5v-9Zm0 0 8 4.5m8-4.5L12 12m0 9v-9"
      : name === "order"
        ? "M6 4h12v16H6zM9 8h6M9 12h6M9 16h4"
        : name === "users"
          ? "M16 20v-1.5A3.5 3.5 0 0 0 12.5 15h-5A3.5 3.5 0 0 0 4 18.5V20m13-9a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm3 9v-1.5a3.5 3.5 0 0 0-2.1-3.2M10 8a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z"
          : "M12 3 3 8l9 5 9-5-9-5Zm-9 9 9 5 9-5M3 16l9 5 9-5";

  return (
    <span className="flex size-10 shrink-0 items-center justify-center rounded-lg border border-primary/20 bg-primary-soft text-primary">
      <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 fill-none stroke-current stroke-[1.8]">
        <path d={path} strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    </span>
  );
}

// Burada gerçek API özetini, günlük operasyon kararlarını destekleyen kompakt metrikler olarak sunuyorum.
export function DashboardOverview({ overview }: { overview: DashboardOverviewData }) {
  const metrics = [
    { label: "Toplam sipariş", value: overview.totalOrderCount.toLocaleString("tr-TR"), note: "Tüm zamanlar", href: "/orders" },
    { label: "Bekleyen sipariş", value: overview.pendingOrderCount.toLocaleString("tr-TR"), note: "İşlem bekliyor" },
    { label: "Ödeme alınan sipariş", value: overview.paidOrderCount.toLocaleString("tr-TR"), note: "İade dışı siparişler" },
    { label: "Net tahsilat", value: formatDashboardCurrency(overview.paidRevenue), note: "Tüm zamanlar" },
    { label: "Aktif ürün", value: overview.activeProductCount.toLocaleString("tr-TR"), note: "Satışa açık katalog", href: "/products" },
    { label: "Düşük stoklu varyant", value: overview.lowStockVariantCount.toLocaleString("tr-TR"), note: "Kontrol gerektirebilir" },
  ];

  return (
    <div>
      <PageHeader
        title="Genel Bakış"
        description="Sipariş, tahsilat ve katalog durumunun güncel özeti."
        actions={<p className="text-xs text-muted">Son güncelleme: {formatDashboardGeneratedAt(overview.generatedAtUtc)}</p>}
      />

      <section aria-label="Operasyon özeti" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {metrics.map((metric) => {
          const content = (
            <>
              <p className="text-sm font-medium text-muted">{metric.label}</p>
              <p className="mt-2 text-2xl font-semibold tracking-tight text-foreground">{metric.value}</p>
              <p className="mt-1 text-xs text-muted">{metric.note}</p>
            </>
          );

          return metric.href ? (
            <Link
              key={metric.label}
              href={metric.href}
              className="rounded-xl border border-border bg-surface p-4 transition-colors hover:border-primary/35 hover:bg-surface-strong focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
            >
              {content}
            </Link>
          ) : (
            <article key={metric.label} className="rounded-xl border border-border bg-surface p-4">
              {content}
            </article>
          );
        })}
      </section>

      <section aria-labelledby="quick-links-title" className="mt-5 overflow-hidden rounded-xl border border-border bg-surface">
        <div className="border-b border-border bg-surface-subtle px-4 py-3 sm:px-5">
          <h2 id="quick-links-title" className="text-base font-semibold text-foreground">Hızlı erişim</h2>
          <p className="mt-1 text-sm text-muted">Sık kullanılan yönetim alanlarına gidin.</p>
        </div>
        <ul className="divide-y divide-border">
          {quickLinks.map((item) => (
            <li key={item.href}>
              <Link href={item.href} className="flex items-center gap-4 px-4 py-4 hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-focus sm:px-5">
                <QuickLinkIcon name={item.icon} />
                <span className="min-w-0 flex-1">
                  <span className="block text-sm font-semibold text-foreground">{item.name}</span>
                  <span className="mt-1 block text-sm leading-5 text-muted">{item.description}</span>
                </span>
                <span aria-hidden="true" className="text-lg text-muted">→</span>
              </Link>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
