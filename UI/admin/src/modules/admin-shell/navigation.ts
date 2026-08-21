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
    status: "in-development",
    items: [
      { label: "Genel Bakış", status: "in-development" },
      { label: "Cari Hesaplar", status: "in-development" },
      { label: "Alış Faturaları", status: "in-development" },
      { label: "Muhasebe Satış Siparişleri", status: "in-development" },
      { label: "Satış Faturaları", status: "in-development" },
      { label: "Ödemeler ve Tahsilatlar", status: "in-development" },
      { label: "Kasa ve Banka", status: "in-development" },
      { label: "Giderler", status: "in-development" },
      { label: "Raporlar", status: "in-development" },
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
