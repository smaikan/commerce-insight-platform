import type { StockMovementDirection, StockMovementListQuery, StockMovementType } from "@/modules/inventory/types";

const pageSizes = [20, 50, 100] as const;
const directions = [1, 2] as const;
const movementTypes = [1, 10, 11, 20, 21, 22, 23, 30, 31, 40, 41, 42, 50, 51, 60] as const;
const datePattern = /^\d{4}-\d{2}-\d{2}$/;
const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

// Burada URL parametrelerini yalnız belgelenmiş stok defteri filtrelerine dönüştürüyorum.
export function parseStockMovementListQuery(params: Record<string, string | string[] | undefined>): StockMovementListQuery {
  const pageNumber = boundedInteger(single(params.pageNumber), 1, 10_000, 1);
  const requestedPageSize = boundedInteger(single(params.pageSize), 1, 100, 20);
  const pageSize = pageSizes.includes(requestedPageSize as (typeof pageSizes)[number]) ? requestedPageSize : 20;
  const createdFrom = validDate(single(params.createdFrom));
  const createdTo = validDate(single(params.createdTo));
  const dateError = createdFrom && createdTo && createdFrom > createdTo ? "Başlangıç tarihi bitiş tarihinden sonra olamaz." : undefined;
  const direction = enumValue(single(params.direction), directions) as StockMovementDirection | undefined;
  const type = enumValue(single(params.type), movementTypes) as StockMovementType | undefined;

  return {
    pageNumber,
    pageSize,
    search: trimmed(single(params.search), 250),
    productVariantId: validUuid(single(params.productVariantId)),
    direction,
    type,
    createdFrom,
    createdTo,
    createdFromUtc: !dateError && createdFrom ? `${createdFrom}T00:00:00.000Z` : undefined,
    createdToUtc: !dateError && createdTo ? `${createdTo}T23:59:59.999Z` : undefined,
    balanceVariantId: validUuid(single(params.balanceVariantId)),
    dateError,
  };
}

// Burada sıfırlama ve boş durum için hangi filtrelerin aktif olduğunu belirliyorum.
export function hasStockMovementFilters(query: StockMovementListQuery): boolean {
  return Boolean(query.search || query.productVariantId || query.direction || query.type || query.createdFrom || query.createdTo);
}

// Burada sayfalama ve mutabakat bağlantılarında filtreleri URL üzerinde koruyorum.
export function buildStockMovementListHref(query: StockMovementListQuery, pageNumber = query.pageNumber): string {
  const params = new URLSearchParams();
  if (pageNumber > 1) params.set("pageNumber", String(pageNumber));
  if (query.pageSize !== 20) params.set("pageSize", String(query.pageSize));
  if (query.search) params.set("search", query.search);
  if (query.productVariantId) params.set("productVariantId", query.productVariantId);
  if (query.direction) params.set("direction", String(query.direction));
  if (query.type) params.set("type", String(query.type));
  if (query.createdFrom) params.set("createdFrom", query.createdFrom);
  if (query.createdTo) params.set("createdTo", query.createdTo);
  if (query.balanceVariantId) params.set("balanceVariantId", query.balanceVariantId);
  const queryString = params.toString();
  return queryString ? `/inventory/stock-movements?${queryString}` : "/inventory/stock-movements";
}

// Burada yinelenen URL değerlerinden yalnız ilkini kabul ediyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada sayfalama değerini API sınırlarında tutuyorum.
function boundedInteger(value: string | undefined, min: number, max: number, fallback: number): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
}

// Burada yalnız izinli enum değerlerini URL filtresine alıyorum.
function enumValue(value: string | undefined, allowed: readonly number[]): number | undefined {
  const parsed = Number(value);
  return allowed.includes(parsed) ? parsed : undefined;
}

// Burada API tarih filtresi için gerçek takvim gününü kabul ediyorum.
function validDate(value: string | undefined): string | undefined {
  if (!value || !datePattern.test(value)) return undefined;
  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));
  return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day ? value : undefined;
}

// Burada GUID filtrelerini geçerli varyant kimlikleriyle sınırlıyorum.
function validUuid(value: string | undefined): string | undefined {
  return value && uuidPattern.test(value) ? value : undefined;
}

// Burada serbest metin aramasını API'nin belgelenen uzunluğunda tutuyorum.
function trimmed(value: string | undefined, maxLength: number): string | undefined {
  const result = value?.trim().slice(0, maxLength);
  return result || undefined;
}
