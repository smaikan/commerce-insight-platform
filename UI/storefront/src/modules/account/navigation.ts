export type AccountDestination = {
  href: string;
  label: string;
  description: string;
  icon: "overview" | "orders" | "returns" | "addresses" | "favorites" | "security";
};

// Burada navbar, mobil menü ve hesap sayfalarının aynı küçük bilgi mimarisini paylaşmasını sağlıyorum.
export const ACCOUNT_DESTINATIONS: readonly AccountDestination[] = [
  { href: "/account", label: "Genel bakış", description: "Hesap alanlarına hızlı erişim", icon: "overview" },
  { href: "/account/orders", label: "Siparişlerim", description: "Sipariş ve teslimat geçmişi", icon: "orders" },
  { href: "/account/returns", label: "İade ve değişim", description: "Taleplerin ve güncel durumları", icon: "returns" },
  { href: "/account/addresses", label: "Adreslerim", description: "Teslimat ve fatura adresleri", icon: "addresses" },
  { href: "/account/favorites", label: "Favorilerim", description: "Kaydettiğin ürünler", icon: "favorites" },
  { href: "/account/security", label: "Güvenlik", description: "Şifre ve aktif oturumlar", icon: "security" },
] as const;
