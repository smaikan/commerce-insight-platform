import type { OrderListQuery, OrderStatus } from "@/modules/orders/types";

// Burada URL'den kabul edilen sayfa boyutu, durum ve tarih biçimi allowlistlerini tanımlıyorum.
const pageSizes = [10, 20, 50, 100] as const;
const orderStatuses = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9] as const;
const datePattern = /^\d{4}-\d{2}-\d{2}$/;

// Burada URL parametrelerini yalnız belgelenmiş sipariş filtreleri ve güvenli sayfalama sınırlarıyla parse ediyorum.
export function parseOrderListQuery(params: Record<string, string | string[] | undefined>): OrderListQuery {
  const pageNumber = boundedInteger(single(params.pageNumber), 1, 10_000, 1);
  const requestedPageSize = boundedInteger(single(params.pageSize), 1, 100, 20);
  const pageSize = pageSizes.includes(requestedPageSize as (typeof pageSizes)[number]) ? requestedPageSize : 20;
  const search = single(params.search)?.trim() || undefined;
  const statusText = single(params.status);
  const statusValue = statusText === undefined || statusText === "" ? Number.NaN : Number(statusText);
  const status = orderStatuses.includes(statusValue as OrderStatus) ? statusValue as OrderStatus : undefined;
  const createdFrom = validDate(single(params.createdFrom));
  const createdTo = validDate(single(params.createdTo));
  const dateError = createdFrom && createdTo && createdFrom > createdTo
    ? "Başlangıç tarihi bitiş tarihinden sonra olamaz."
    : undefined;

  return {
    pageNumber,
    pageSize,
    search,
    status,
    createdFrom,
    createdTo,
    createdFromUtc: !dateError && createdFrom ? `${createdFrom}T00:00:00.000Z` : undefined,
    createdToUtc: !dateError && createdTo ? `${createdTo}T23:59:59.999Z` : undefined,
    dateError,
  };
}

// Burada filtre varlığını boş sonuç metni ve temizleme aksiyonu için tek biçimde belirliyorum.
export function hasOrderFilters(query: OrderListQuery): boolean {
  return Boolean(query.search) || query.status !== undefined || Boolean(query.createdFrom) || Boolean(query.createdTo);
}

// Burada sayfalama bağlantılarında mevcut sipariş filtrelerini URL üzerinde koruyorum.
export function buildOrderListHref(query: OrderListQuery, pageNumber: number): string {
  const params = new URLSearchParams();
  if (pageNumber > 1) params.set("pageNumber", String(pageNumber));
  if (query.pageSize !== 20) params.set("pageSize", String(query.pageSize));
  if (query.search) params.set("search", query.search);
  if (query.status !== undefined) params.set("status", String(query.status));
  if (query.createdFrom) params.set("createdFrom", query.createdFrom);
  if (query.createdTo) params.set("createdTo", query.createdTo);
  const queryString = params.toString();
  return queryString ? `/orders?${queryString}` : "/orders";
}

// Burada yinelenebilen Next.js search param değerlerinden yalnız ilk metin değerini alıyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada sayısal URL değerini belgelenmiş alt ve üst sınırlar içinde tutuyorum.
function boundedInteger(value: string | undefined, min: number, max: number, fallback: number): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
}

// Burada HTML tarih filtresini gerçek bir takvim günü olduğunda kabul ediyorum.
function validDate(value: string | undefined): string | undefined {
  if (!value || !datePattern.test(value)) return undefined;
  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));
  return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day
    ? value
    : undefined;
}
