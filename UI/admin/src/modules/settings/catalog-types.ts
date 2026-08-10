import type { components } from "@/generated/api";
import type { PagedResult } from "@/lib/api/pagination";

// Burada katalog tanımlarının wire tiplerini üretilmiş OpenAPI şemalarına bağlıyorum.
export type ProductType = components["schemas"]["ProductTypeDto"];
export type Tag = components["schemas"]["TagDto"];
export type CatalogItem = ProductType | Tag;
export type CatalogPage = PagedResult<CatalogItem>;

// Burada üç farklı katalog formunun ortak ve kaynak bazlı alanlarını taşıyorum.
export type CatalogFormValue = {
  name: string;
  url?: string | null;
  description?: string | null;
  isActive: boolean;
};
