import type { CustomerListQuery, UserStatus } from "@/modules/customers/types";

// Burada belgelenmiş rol ve durum enum değerlerini allowlist olarak tanımlıyorum.
const userStatuses = [1, 2, 3] as const;
const pageSizes = [10, 20, 50, 100] as const;

// Burada URL parametrelerini yalnız belgelenmiş müşteri filtreleri ve güvenli sayfalama sınırlarıyla parse ediyorum.
export function parseCustomerListQuery(
  params: Record<string, string | string[] | undefined>,
): CustomerListQuery {
  const pageNumber = boundedInteger(single(params.pageNumber), 1, 10_000, 1);
  const requestedPageSize = boundedInteger(single(params.pageSize), 1, 100, 20);
  const pageSize = (pageSizes as readonly number[]).includes(requestedPageSize)
    ? requestedPageSize
    : 20;

  const search = single(params.search)?.trim() || undefined;

  const statusText = single(params.status);
  const statusValue = statusText === undefined || statusText === "" ? Number.NaN : Number(statusText);
  const status = (userStatuses as readonly number[]).includes(statusValue)
    ? (statusValue as UserStatus)
    : undefined;

  return { pageNumber, pageSize, search, role: 1, status };
}

// Burada filtre varlığını boş sonuç metni ve temizleme aksiyonu için tek biçimde belirliyorum.
export function hasCustomerFilters(query: CustomerListQuery): boolean {
  return Boolean(query.search) || query.status !== undefined;
}

// Burada sayfalama bağlantılarında mevcut müşteri filtrelerini URL üzerinde koruyorum.
export function buildCustomerListHref(query: CustomerListQuery, pageNumber: number): string {
  const params = new URLSearchParams();
  if (pageNumber > 1) params.set("pageNumber", String(pageNumber));
  if (query.pageSize !== 20) params.set("pageSize", String(query.pageSize));
  if (query.search) params.set("search", query.search);
  if (query.status !== undefined) params.set("status", String(query.status));
  const qs = params.toString();
  return qs ? `/customers?${qs}` : "/customers";
}

// Burada yinelenebilen Next.js search param değerlerinden yalnız ilk metin değerini alıyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada sayısal URL değerini belgelenmiş alt ve üst sınırlar içinde tutuyorum.
function boundedInteger(
  value: string | undefined,
  min: number,
  max: number,
  fallback: number,
): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
}
