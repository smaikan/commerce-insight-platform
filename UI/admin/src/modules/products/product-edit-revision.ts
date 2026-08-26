import type { Product } from "@/modules/products/types";
import { editableVariantRevision } from "@/modules/products/variant-editing";

type ProductRevisionSource = Pick<Product, "id" | "status" | "hasVariants" | "mainSku" | "variants">;

export type ProductFormIdentity = {
  productId: string;
  revision: string;
};

// Burada formun authoritative ürün/varyant verileri değiştiğinde yenilenmesini sağlayan sunucu kimliğini üretiyorum.
export function productFormIdentity(product: ProductRevisionSource): ProductFormIdentity {
  return {
    productId: product.id,
    revision: `${product.status}:${product.hasVariants}:${product.mainSku}:${editableVariantRevision(product.variants)}`,
  };
}

// Burada aynı üründeki kaydedilmemiş taslağı Server Action revalidation'ından koruyup farklı ürüne taşınmasını engelliyorum.
export function reconcileProductFormIdentity(
  current: ProductFormIdentity,
  incoming: ProductFormIdentity,
  hasUnsavedDraft: boolean,
): ProductFormIdentity {
  if (current.productId !== incoming.productId) return incoming;
  if (hasUnsavedDraft) return current;
  if (current.revision === incoming.revision) return current;
  return incoming;
}
