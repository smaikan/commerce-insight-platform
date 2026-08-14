// Burada sunucu tarafında hazırlanan küçük, seri hale getirilebilir navigasyon öğesi sözleşmesini tanımlıyorum.
export type StorefrontNavigationItem = {
  id: string;
  label: string;
  href: string;
  productCount: number;
};

// Burada masaüstü ve mobil navigasyonun ortak bilgi mimarisini tek tipte tutuyorum.
export type StorefrontNavigationGroup = {
  id: "categories" | "collections" | "brands";
  label: string;
  href?: string;
  items: StorefrontNavigationItem[];
};
