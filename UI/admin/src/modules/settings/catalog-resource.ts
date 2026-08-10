export type CatalogResource = "product-types" | "tags";

export type CatalogResourceConfig = {
  resource: CatalogResource;
  title: string;
  singularTitle: string;
  description: string;
  endpoint: string;
  supportsUrl: boolean;
  supportsDescription: boolean;
};

// Burada generic kalan iki katalog tanımının gerçek API alanlarını ve ekran metinlerini tek açık haritada tutuyorum.
export const catalogResourceConfigs: Record<CatalogResource, CatalogResourceConfig> = {
  "product-types": { resource: "product-types", title: "Ürün türleri", singularTitle: "Ürün türü", description: "Ürünlerin temel katalog sınıflandırmalarını yönetin.", endpoint: "/api/product-types", supportsUrl: false, supportsDescription: true },
  tags: { resource: "tags", title: "Etiketler", singularTitle: "Etiket", description: "Ürünleri aranabilir ve tekrar kullanılabilir etiketlerle düzenleyin.", endpoint: "/api/tags", supportsUrl: true, supportsDescription: false },
};

// Burada URL segmentinin yalnızca desteklenen katalog kaynaklarından biri olmasını doğruluyorum.
export function isCatalogResource(value: string): value is CatalogResource {
  return value === "product-types" || value === "tags";
}
