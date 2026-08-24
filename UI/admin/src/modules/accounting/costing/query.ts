import type { CostingQuery } from "./types";

// Burada maliyet çalışma alanını yalnız belgelenmiş arama ve varyant kimliğiyle sınırlandırıyorum.
export function parseCostingQuery(params: Record<string, string | string[] | undefined>): CostingQuery {
  return {
    search: text(params.search).slice(0, 100),
    productVariantId: uuid(params.productVariantId),
  };
}

// Burada seçili varyantı ve arama bağlamını paylaşılabilir URL üzerinde koruyorum.
export function buildCostingHref(query: CostingQuery): string {
  const params = new URLSearchParams();
  if (query.search) params.set("search", query.search);
  if (query.productVariantId) params.set("productVariantId", query.productVariantId);
  const encoded = params.toString();
  return encoded ? `/accounting/costing?${encoded}` : "/accounting/costing";
}

function text(value: string | string[] | undefined): string {
  return (Array.isArray(value) ? value[0] : value || "").trim();
}

function uuid(value: string | string[] | undefined): string | null {
  const candidate = text(value);
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(candidate) ? candidate : null;
}
