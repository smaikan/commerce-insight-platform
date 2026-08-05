import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type {
  Brand,
  PagedResult,
  Product,
  ProductFormOptions,
  ProductImage,
  ProductListQuery,
  ProductType,
  ProductVariant,
  TaxRate,
} from "@/modules/products/types";

export type ProductVariantInput = {
  id?: string;
  name: string;
  value: string;
  sku: string;
  price: number;
  stock: number;
  compareAtPrice?: number | null;
  barcode?: string | null;
  material?: string | null;
  isActive: boolean;
  openingUnitCostExcludingVat?: number | null;
  openingUnitCostIncludingVat?: number | null;
  stockAdjustmentReason?: string | null;
};

// Burada güncel runtime/Markdown sözleşmesinin OpenAPI'den ileride olan ürün oluşturma gövdesini açıkça tanımlıyorum.
export type CreateProductInput = {
  title: string;
  mainSku: string;
  hasVariants: boolean;
  type?: string | null;
  url?: string | null;
  brandId?: string | null;
  description?: string | null;
  status: 0 | 1 | 2 | 3;
  isFeatured: boolean;
  displayOrder: number;
  seoTitle?: string | null;
  seoDescription?: string | null;
  collections: string[];
  variants?: ProductVariantInput[];
  tags: string[];
  taxRateId?: string | null;
};

export type UpdateProductInput = Omit<
  CreateProductInput,
  "hasVariants" | "status" | "isFeatured" | "collections" | "variants"
>;

export type ProductImageInput = {
  imageUrl: string;
  altText?: string | null;
  displayOrder: number;
  isMain: boolean;
};

// Burada mevcut backend filtrelerini sunucu tarafı sayfalama ile ürün listesi isteğine çeviriyorum.
export function getProducts(query: ProductListQuery, session: AdminSession): Promise<PagedResult<Product>> {
  const params = new URLSearchParams({
    PageNumber: String(query.pageNumber),
    PageSize: String(query.pageSize),
    SortBy: String(query.sortBy),
    Descending: String(query.descending),
  });
  if (query.search) params.set("Search", query.search);
  if (query.typeId) params.set("TypeId", query.typeId);
  if (query.brandId) params.set("BrandId", query.brandId);
  if (query.status !== undefined) params.set("Status", String(query.status));
  if (query.isFeatured !== undefined) params.set("IsFeatured", String(query.isFeatured));
  return apiRequest(`/api/products?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada filtre seçenekleri için belgelenen ilk 100 ürün tipi ve markayı birlikte getiriyorum.
export async function getProductListOptions(session: AdminSession): Promise<{ productTypes: ProductType[]; brands: Brand[] }> {
  const [types, brands] = await Promise.all([
    apiRequest<PagedResult<ProductType>>("/api/product-types?PageNumber=1&PageSize=100", { accessToken: session.accessToken }),
    apiRequest<PagedResult<Brand>>("/api/brands?PageNumber=1&PageSize=100", { accessToken: session.accessToken }),
  ]);
  return { productTypes: types.items, brands: brands.items };
}

// Burada ürün detayını public ürün kimliğiyle ve cache paylaşmadan getiriyorum.
export function getProduct(productId: string, session: AdminSession): Promise<Product> {
  return apiRequest(`/api/products/${encodeURIComponent(productId)}`, { accessToken: session.accessToken });
}

// Burada ürün görsellerini düzenleme ekranı için tek ek istekte getiriyorum.
export function getProductImages(productId: string, session: AdminSession): Promise<PagedResult<ProductImage>> {
  return apiRequest(`/api/product-images/by-product/${encodeURIComponent(productId)}?pageNumber=1&pageSize=100`, { accessToken: session.accessToken });
}

// Burada liste DTO'sunda görsel bulunmadığı için mevcut sayfadaki ürün görsellerini API'yi yormayan sınırlı işçilerle topluyorum.
export async function getProductMainImages(productIds: string[], session: AdminSession): Promise<Record<string, ProductImage | null>> {
  const result: Record<string, ProductImage | null> = {};
  let cursor = 0;

  // Burada aynı anda en fazla altı ürün görseli isteği çalıştırarak yüksek sayfa boyutlarında ani istek yükünü sınırlıyorum.
  const worker = async () => {
    while (cursor < productIds.length) {
      const productId = productIds[cursor];
      cursor += 1;
      try {
        const page = await getProductImages(productId, session);
        result[productId] = page.items.find((image) => image.isMain) || page.items[0] || null;
      } catch {
        result[productId] = null;
      }
    }
  };

  await Promise.all(Array.from({ length: Math.min(6, productIds.length) }, () => worker()));
  return result;
}

// Burada form seçeneklerini ve anonim okunabilen aktif vergi oranlarını birbirinden bağımsız yüklüyorum.
export async function getProductFormOptions(session: AdminSession): Promise<ProductFormOptions> {
  const [brandsResult, taxRatesResult] = await Promise.allSettled([
    apiRequest<PagedResult<Brand>>("/api/brands?PageNumber=1&PageSize=100", { accessToken: session.accessToken }),
    apiRequest<PagedResult<TaxRate>>("/api/tax-rates/active?pageNumber=1&pageSize=100", { accessToken: session.accessToken }),
  ]);

  return {
    brands: brandsResult.status === "fulfilled" ? brandsResult.value.items : [],
    taxRates: taxRatesResult.status === "fulfilled" ? taxRatesResult.value.items : [],
    taxRatesUnavailable: taxRatesResult.status === "rejected",
  };
}

// Burada yeni ürünü yönetici yetkili API sınırında oluşturuyorum.
export function createProduct(input: CreateProductInput, session: AdminSession): Promise<Product> {
  return apiRequest("/api/products", { method: "POST", body: input, accessToken: session.accessToken });
}

// Burada ürünün temel alanlarını ve etiketlerini güncel runtime sözleşmesiyle değiştiriyorum.
export function updateProduct(productId: string, input: UpdateProductInput, session: AdminSession): Promise<Product> {
  return apiRequest(`/api/products/${encodeURIComponent(productId)}`, {
    method: "PUT",
    body: input,
    accessToken: session.accessToken,
  });
}

// Burada ürünün ayrı yönetilen durum alanlarından yalnız istenen birini güncelliyorum.
export function patchProductState(
  productId: string,
  segment: "status" | "featured" | "has-variants",
  body: Record<string, boolean | number>,
  session: AdminSession,
): Promise<Product> {
  return apiRequest(`/api/products/${encodeURIComponent(productId)}/${segment}`, {
    method: "PATCH",
    body,
    accessToken: session.accessToken,
  });
}

// Burada mevcut varyantın bilgi, fiyat, stok sayımı ve aktivasyon alanlarını tek belgelenmiş komutla güncelliyorum.
export function updateProductVariant(variant: ProductVariantInput & { id: string }, session: AdminSession): Promise<ProductVariant> {
  const body = {
    name: variant.name,
    value: variant.value,
    sku: variant.sku,
    price: variant.price,
    stock: variant.stock,
    compareAtPrice: variant.compareAtPrice,
    barcode: variant.barcode,
    material: variant.material,
    isActive: variant.isActive,
    stockAdjustmentReason: variant.stockAdjustmentReason,
  };
  return apiRequest(`/api/product-variants/${encodeURIComponent(variant.id)}`, {
    method: "PUT",
    body,
    accessToken: session.accessToken,
  });
}

// Burada düzenleme ekranından eklenen yeni varyantı açılış stoğuyla ürüne bağlıyorum.
export function createProductVariant(productId: string, variant: ProductVariantInput, session: AdminSession): Promise<ProductVariant> {
  const body = {
    name: variant.name,
    value: variant.value,
    sku: variant.sku,
    price: variant.price,
    stock: variant.stock,
    compareAtPrice: variant.compareAtPrice,
    barcode: variant.barcode,
    material: variant.material,
    isActive: variant.isActive,
  };
  return apiRequest(`/api/product-variants/by-product/${encodeURIComponent(productId)}`, {
    method: "POST",
    body,
    accessToken: session.accessToken,
  });
}

// Burada URL tabanlı ürün görselini ürün oluşturulduktan sonra ayrı API işlemiyle kaydediyorum.
export function createProductImage(productId: string, input: ProductImageInput, session: AdminSession): Promise<ProductImage> {
  return apiRequest(`/api/product-images/by-product/${encodeURIComponent(productId)}`, {
    method: "POST",
    body: input,
    accessToken: session.accessToken,
  });
}
