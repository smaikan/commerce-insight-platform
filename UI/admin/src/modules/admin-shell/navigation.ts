export type NavigationItem = {
  label: string;
  href?: string;
  status: "available" | "next" | "planned" | "future" | "in-development";
};

export type NavigationSection = {
  label: string;
  items: NavigationItem[];
  collapsible?: boolean;
  status?: NavigationItem["status"];
};

// Burada menüyü API/controller adlarına göre değil, yöneticinin günlük görev akışına göre grupluyorum.
export const navigationSections: NavigationSection[] = [
  {
    label: "Genel",
    items: [{ label: "Genel Bakış", href: "/dashboard", status: "available" }],
  },
  {
    label: "Satış",
    items: [
      { label: "Siparişler", href: "/orders", status: "available" },
      { label: "İletişim Mesajları", href: "/contact-messages", status: "available" },
      { label: "Müşteriler", href: "/customers", status: "available" },
      { label: "İndirimler", href: "/coupons", status: "available" },
    ],
  },
  {
    label: "Katalog ve Stok",
    items: [
      { label: "Ürünler", href: "/products", status: "available" },
      { label: "Koleksiyonlar", href: "/collections", status: "available" },
      { label: "Markalar", href: "/brands", status: "available" },
      { label: "Stok İşlemleri", href: "/inventory/stock-movements", status: "available" },
    ],
  },
  {
    label: "Vitrin",
    items: [
      { label: "Bannerlar", href: "/banners", status: "available" },
    ],
  },
  {
    label: "Pazarlama",
    items: [
      { label: "Google Analytics", href: "/marketing/google-analytics", status: "in-development" },
      { label: "Meta Reklam Yönetimi", href: "/marketing/meta-ads", status: "in-development" },
    ],
  },
  {
    label: "Muhasebe",
    collapsible: true,
    status: "available",
    items: [
      { label: "Genel Bakış", href: "/accounting", status: "available" },
      { label: "Cari Hesaplar", href: "/accounting/current-accounts", status: "available" },
      { label: "Alış Faturaları", href: "/accounting/purchase-invoices", status: "available" },
      { label: "Muhasebe Satışları", href: "/accounting/sales-orders", status: "available" },
      { label: "Satış Faturaları", href: "/accounting/sales-invoices", status: "available" },
      { label: "Ödemeler ve Tahsilatlar", href: "/accounting/payments", status: "available" },
      { label: "Kasa ve Banka", href: "/accounting/treasury", status: "available" },
      { label: "Giderler", href: "/accounting/expenses", status: "available" },
      { label: "FIFO Maliyet", href: "/accounting/costing", status: "available" },
      { label: "Raporlar", href: "/accounting/reports", status: "available" },
    ],
  },
  {
    label: "Pazaryeri Entegrasyonları",
    collapsible: true,
    status: "future",
    items: [
      { label: "Bağlantılar", status: "future" },
      { label: "Ürün ve Sipariş Senkronu", status: "future" },
    ],
  },
  {
    label: "Sistem",
    items: [
      { label: "Yöneticiler", href: "/managers", status: "available" },
      { label: "Ayarlar", href: "/settings", status: "available" },
    ],
  },
];

export function navigationStatusLabel(status: NavigationItem["status"]): string {
  switch (status) {
    case "in-development":
      return "Geliştirme aşamasında";
    case "next":
      return "Sırada";
    case "planned":
      return "Planlı";
    case "future":
      return "Yakında";
    default:
      return "";
  }
}

function isCurrentNavigationItem(pathname: string, href: string | undefined): boolean {
  return Boolean(href && (pathname === href || pathname.startsWith(`${href}/`)));
}

// Birbirini kapsayan rotalarda yalnız en özgül menü öğesini seçili tutar.
export function getCurrentNavigationHref(pathname: string): string | undefined {
  return navigationSections
    .flatMap((section) => section.items)
    .map((item) => item.href)
    .filter((href): href is string => isCurrentNavigationItem(pathname, href))
    .sort((left, right) => right.length - left.length)[0];
}
