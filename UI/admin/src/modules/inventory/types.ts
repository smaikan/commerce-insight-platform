import type { components } from "@/generated/api";

// Burada stok defteri ve mutabakat cevaplarını güncel OpenAPI şemalarına bağlıyorum.
export type StockMovement = components["schemas"]["StockMovementListItemDto"];
export type StockMovementPage = components["schemas"]["StockMovementListItemDtoPagedResult"];
export type StockBalance = components["schemas"]["StockBalanceDto"];
export type StockMovementDirection = components["schemas"]["StockMovementDirection"];
export type StockMovementType = components["schemas"]["StockMovementType"];
export type BulkStockMovement = components["schemas"]["BulkStockMovementRequest"];
export type BulkStockMovementResult = components["schemas"]["BulkCreateStockMovementsResultDto"];

// Burada URL'den çözümlenmiş stok defteri filtrelerini tek okunabilir modelde taşıyorum.
export type StockMovementListQuery = {
  pageNumber: number;
  pageSize: number;
  search?: string;
  productVariantId?: string;
  direction?: StockMovementDirection;
  type?: StockMovementType;
  createdFrom?: string;
  createdTo?: string;
  createdFromUtc?: string;
  createdToUtc?: string;
  balanceVariantId?: string;
  dateError?: string;
};

// Burada toplu hareket formunun kullanıcıya gösterilecek güvenli sonucunu tanımlıyorum.
export type StockMovementActionState = {
  status: "idle" | "error" | "success";
  message?: string;
  traceId?: string;
  movementCount?: number;
  completionToken?: string;
  fieldErrors?: Record<string, string[]>;
};

export const initialStockMovementActionState: StockMovementActionState = { status: "idle" };
