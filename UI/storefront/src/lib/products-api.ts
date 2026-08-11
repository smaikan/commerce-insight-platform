// Burada eski import kullanan sitemap ve testlerin yeni ürün modülüne güvenli geçişini koruyorum.
export { getAllProductSeoIndex, getPublishedProductBySlug as getProductBySlug } from "@/modules/product/api";
export type { ProductSeoData as ProductSeoResponse } from "@/modules/product/types";
