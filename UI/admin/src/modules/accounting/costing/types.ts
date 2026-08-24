import type { components } from "@/generated/api";

export type OpeningBalanceCostLayer = components["schemas"]["OpeningBalanceCostLayerDto"];
export type ProductVariantCostHistory = components["schemas"]["ProductVariantCostHistoryDto"];

export type CostingVariantOption = {
  id: string;
  productId: string;
  productName: string;
  variantName: string;
  sku: string;
  stock: number;
  isActive: boolean;
};

export type CostingQuery = {
  search: string;
  productVariantId: string | null;
};

export type OpeningCostDraft = {
  layerId: string;
  productVariantId: string;
  expectedConcurrencyToken: string;
  unitCostExcludingVat: string;
  unitCostIncludingVat: string;
};

export type OpeningCostActionState = {
  status: "idle" | "error" | "conflict" | "success";
  message?: string;
  draft?: OpeningCostDraft;
  currentLayer?: OpeningBalanceCostLayer;
  fieldErrors?: Record<string, string[]>;
  traceId?: string;
  refresh?: boolean;
};

export const initialOpeningCostActionState: OpeningCostActionState = { status: "idle" };
