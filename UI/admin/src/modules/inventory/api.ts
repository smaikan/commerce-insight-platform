import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { BulkStockMovement, BulkStockMovementResult, StockBalance, StockMovementListQuery, StockMovementPage } from "@/modules/inventory/types";

// Burada stok defterini yalnız belgelenen filtrelerle ve kullanıcıya özel cache olmadan okuyorum.
export function getStockMovements(query: StockMovementListQuery, session: AdminSession): Promise<StockMovementPage> {
  const params = new URLSearchParams({ PageNumber: String(query.pageNumber), PageSize: String(query.pageSize) });
  if (query.search) params.set("Search", query.search);
  if (query.productVariantId) params.set("ProductVariantId", query.productVariantId);
  if (query.direction) params.set("Direction", String(query.direction));
  if (query.type) params.set("Type", String(query.type));
  if (query.createdFromUtc) params.set("CreatedFromUtc", query.createdFromUtc);
  if (query.createdToUtc) params.set("CreatedToUtc", query.createdToUtc);
  return apiRequest(`/api/stock-movements?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada seçili varyantın kayıtlı stoğu ile defter toplamını mutabakat için getiriyorum.
export function getStockBalance(productVariantId: string, session: AdminSession): Promise<StockBalance> {
  return apiRequest(`/api/stock-movements/variants/${encodeURIComponent(productVariantId)}/balance`, { accessToken: session.accessToken });
}

// Burada en fazla 500 satırlık hareketi backend'in tek atomik bulk işlemine gönderiyorum.
export function createBulkStockMovements(movements: BulkStockMovement[], session: AdminSession): Promise<BulkStockMovementResult> {
  return apiRequest<BulkStockMovementResult>("/api/stock-movements/bulk", { method: "POST", body: { movements }, accessToken: session.accessToken });
}
