import type { ProductStatus } from "@/modules/products/types";

// Burada yalnız taslak ve aktif ürünler için ters hızlı durum hedefini hesaplıyorum.
export function getQuickProductStatus(status: ProductStatus): ProductStatus | null {
  if (status === 1) return 0;
  if (status === 0) return 1;
  return null;
}
