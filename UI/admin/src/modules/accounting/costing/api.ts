import "server-only";

import type { components } from "@/generated/api";
import { apiRequest } from "@/lib/api/client";
import { ApiError } from "@/lib/api/problem";
import type { PagedResult } from "@/lib/api/pagination";
import type { AdminSession } from "@/lib/auth/contracts";
import type { CostingVariantOption, OpeningBalanceCostLayer, ProductVariantCostHistory } from "./types";

type Product = components["schemas"]["ProductDto"];
type ProductVariant = components["schemas"]["ProductVariantDto"];

// Burada muhasebe maliyet ekranının ürün aramasını kendi adapter sınırında varyant seçeneklerine dönüştürüyorum.
export async function searchCostingVariants(search: string, session: AdminSession): Promise<{ items: CostingVariantOption[]; truncated: boolean }> {
  const params = new URLSearchParams({ PageNumber: "1", PageSize: "20", SortBy: "1", Descending: "false" });
  if (search) params.set("Search", search);
  const products = await apiRequest<PagedResult<Product>>(`/api/products?${params}`, { accessToken: session.accessToken });
  return {
    items: products.items.flatMap((product) => product.variants.map((variant) => mapVariant(variant, product.title))),
    truncated: products.totalCount > products.items.length,
  };
}

// Burada doğrudan URL ile açılan varyantı ürün listesinden bağımsız olarak doğruluyorum.
export async function getCostingVariant(id: string, known: CostingVariantOption[], session: AdminSession): Promise<CostingVariantOption> {
  const found = known.find((item) => item.id === id);
  if (found) return found;
  const variant = await apiRequest<ProductVariant>(`/api/product-variants/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
  return mapVariant(variant, variant.productId);
}

// Burada açılış katmanı olmayan varyantı düzenlenemez ama geçerli bir maliyet çalışma alanı olarak ele alıyorum.
export async function getOpeningBalanceLayer(productVariantId: string, session: AdminSession): Promise<OpeningBalanceCostLayer | null> {
  try {
    return await apiRequest(`/api/accounting/inventory-cost-layers/opening-balance/by-variant/${encodeURIComponent(productVariantId)}`, { accessToken: session.accessToken });
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) return null;
    throw error;
  }
}

export function updateOpeningBalanceLayer(id: string, input: components["schemas"]["UpdateOpeningBalanceCostLayerRequest"], session: AdminSession): Promise<OpeningBalanceCostLayer> {
  return apiRequest(`/api/accounting/inventory-cost-layers/${encodeURIComponent(id)}/opening-balance-cost`, { method: "PATCH", body: input, accessToken: session.accessToken });
}

export function getVariantCostHistory(productVariantId: string, session: AdminSession): Promise<ProductVariantCostHistory[]> {
  return apiRequest(`/api/accounting/product-variants/${encodeURIComponent(productVariantId)}/cost-history`, { accessToken: session.accessToken });
}

function mapVariant(variant: ProductVariant, productName: string): CostingVariantOption {
  return { id: variant.id, productId: variant.productId, productName, variantName: `${variant.name}: ${variant.value}`, sku: variant.sku, stock: variant.stock, isActive: variant.isActive };
}
