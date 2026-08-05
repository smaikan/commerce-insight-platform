export type NavigationItem = {
  label: string;
  href?: string;
  status: "available" | "next" | "planned" | "future";
};

export type NavigationSection = {
  label: string;
  items: NavigationItem[];
  defaultOpen?: boolean;
};

export const navigationSections: NavigationSection[] = [
  {
    label: "Genel Bakış",
    defaultOpen: true,
    items: [{ label: "Dashboard", href: "/dashboard", status: "available" }],
  },
  {
    label: "Ticaret",
    defaultOpen: true,
    items: [
      { label: "Siparişler", href: "/orders", status: "available" },
      { label: "Ürünler", href: "/products", status: "available" },
      { label: "Koleksiyonlar", status: "planned" },
      { label: "Kampanyalar / Kuponlar", status: "planned" },
    ],
  },
  {
    label: "Operasyonlar",
    items: [
      { label: "Stok İşlemleri", status: "planned" },
      { label: "Müşteriler", status: "planned" },
    ],
  },
  {
    label: "Muhasebe",
    items: [
      { label: "Genel Bakış", status: "planned" },
      { label: "Cari Hesaplar", status: "planned" },
      { label: "Alış ve Satış Belgeleri", status: "planned" },
      { label: "Ödemeler ve Tahsilatlar", status: "planned" },
      { label: "Raporlar", status: "planned" },
    ],
  },
  {
    label: "Pazaryeri Entegrasyonları",
    items: [
      { label: "Bağlantılar", status: "future" },
      { label: "Ürün ve Sipariş Senkronu", status: "future" },
    ],
  },
  {
    label: "Sistem",
    items: [
      { label: "Yöneticiler", status: "planned" },
      { label: "Ayarlar", status: "future" },
    ],
  },
];

export function navigationStatusLabel(status: NavigationItem["status"]): string {
  switch (status) {
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
