export type SettingsOption = {
  title: string;
  description: string;
  href?: string;
  status: "available" | "in-development";
};

export type SettingsGroup = {
  title: string;
  description: string;
  options: SettingsOption[];
};

// Burada gerçek rotaları ve gelecekteki ayar kapsamını tek bilgi mimarisinde topluyorum.
export const settingsGroups: SettingsGroup[] = [
  {
    title: "Genel",
    description: "Mağaza kimliği ve bölgesel gösterim tercihleri.",
    options: [
      { title: "Mağaza ayarları", description: "Mağaza kimliği, iletişim, yasal bilgiler, SEO ve storefront tercihleri.", href: "/settings/store", status: "available" },
      { title: "Yerelleştirme", description: "Dil, saat dilimi, tarih ve sayı biçimleri.", status: "in-development" },
    ],
  },
  {
    title: "Sipariş ve teslimat",
    description: "Sipariş akışı, kargo seçenekleri ve satış sonrası kuralları.",
    options: [
      { title: "Sipariş ayarları", description: "Numaralandırma, varsayılan durum ve işlem kuralları.", status: "in-development" },
      { title: "Kargo yöntemleri", description: "Checkout'ta sunulan yöntemleri, ücretleri ve sıralamayı yönetin.", href: "/settings/shipping-methods", status: "available" },
      { title: "İade ve iptal politikaları", description: "Süreleri, nedenleri ve müşteri koşullarını belirleyin.", status: "in-development" },
    ],
  },
  {
    title: "Ürün ve stok",
    description: "Ürün yaşam döngüsü ve stok operasyonu tercihleri.",
    options: [
      { title: "Stok ayarları", description: "Düşük stok eşiği, rezervasyon ve satış kuralları.", status: "in-development" },
      { title: "Koleksiyonlar", description: "Manuel koleksiyonları, görünürlüklerini ve vitrin durumlarını yönetin.", href: "/collections", status: "available" },
      { title: "Markalar", description: "Marka kimliklerini, görsellerini ve kullanılabilirliklerini yönetin.", href: "/brands", status: "available" },
      { title: "Katalog tanımları", description: "Ürün türü ve etiket yönetimine tek noktadan erişin.", href: "/settings/catalog/product-types", status: "available" },
    ],
  },
  {
    title: "Vergi ve ödeme",
    description: "Vergi hesaplama kaynakları ve ödeme sağlayıcıları.",
    options: [
      { title: "Vergi oranları", description: "Ürünlerde kullanılan vergi oranlarını yönetin.", href: "/settings/tax-rates", status: "available" },
      { title: "Ödeme yöntemleri", description: "Ödeme sağlayıcıları, modlar ve bağlantı durumları.", status: "in-development" },
    ],
  },
  {
    title: "Bildirimler",
    description: "Müşteri ve yönetici iletişim tercihleri.",
    options: [
      { title: "E-posta ayarları", description: "Gönderen bilgileri ve teslimat yapılandırması.", status: "in-development" },
      { title: "Bildirim tercihleri", description: "Olay bazlı müşteri ve operasyon bildirimleri.", status: "in-development" },
    ],
  },
  {
    title: "Kullanıcılar ve güvenlik",
    description: "Kişisel hesap ve yönetim erişimi ayarları.",
    options: [
      { title: "Hesabım", description: "Profilinizi, e-posta adresinizi ve parolanızı yönetin.", href: "/settings/account", status: "available" },
      { title: "Yöneticiler", description: "Panel erişimi olan Admin hesaplarını görüntüleyin ve yeni yönetici ekleyin.", href: "/managers", status: "available" },
      { title: "Roller ve yetkiler", description: "Yönetim modülleri için ayrıntılı erişim politikaları.", status: "in-development" },
      { title: "Oturumlar ve güvenlik", description: "Aktif cihazları görüntüleyin ve hesap oturumlarını sonlandırın.", href: "/settings/security", status: "available" },
    ],
  },
  {
    title: "Sistem",
    description: "Harici bağlantılar ve operasyon görünürlüğü.",
    options: [
      { title: "Entegrasyonlar", description: "Kargo, ödeme ve iş uygulaması bağlantıları.", status: "in-development" },
      { title: "Webhook'lar", description: "Olay abonelikleri ve gönderim geçmişi.", status: "in-development" },
      { title: "Sistem durumu", description: "Servislerin yapılandırma ve bağlantı sağlığı.", status: "in-development" },
    ],
  },
];

// Burada kullanılabilir ayar rotalarını alt sayfa navigasyonu için sadeleştiriyorum.
export const availableSettingsOptions = settingsGroups.flatMap((group) =>
  group.options.filter((option): option is SettingsOption & { href: string } => Boolean(option.href)),
);
