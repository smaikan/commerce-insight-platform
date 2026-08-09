import type { CouponListQuery } from "@/modules/coupons/types";

const pageSizes = [20, 50, 100] as const;

// Burada URL parametrelerini kupon sözleşmesindeki dar filtre modeline dönüştürüyorum.
export function parseCouponListQuery(params: Record<string, string | string[] | undefined>): CouponListQuery {
  const requestedPageSize = boundedInteger(single(params.pageSize), 1, 100, 20);
  return {
    pageNumber: boundedInteger(single(params.pageNumber), 1, 10_000, 1),
    pageSize: pageSizes.includes(requestedPageSize as (typeof pageSizes)[number]) ? requestedPageSize : 20,
    isActive: booleanValue(single(params.isActive)),
  };
}

// Burada sayfalama bağlantılarında seçili aktiflik filtresini koruyorum.
export function buildCouponListHref(query: CouponListQuery, pageNumber = query.pageNumber): string {
  const params = new URLSearchParams();
  if (pageNumber > 1) params.set("pageNumber", String(pageNumber));
  if (query.pageSize !== 20) params.set("pageSize", String(query.pageSize));
  if (query.isActive !== undefined) params.set("isActive", String(query.isActive));
  const queryString = params.toString();
  return queryString ? `/coupons?${queryString}` : "/coupons";
}

// Burada yinelenen query değerlerinden yalnız ilkini kabul ediyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada sayfalama değerlerini API'nin makul sınırlarında tutuyorum.
function boundedInteger(value: string | undefined, min: number, max: number, fallback: number): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
}

// Burada aktiflik filtresine yalnız açık boolean değerlerini alıyorum.
function booleanValue(value: string | undefined): boolean | undefined {
  if (value === "true") return true;
  if (value === "false") return false;
  return undefined;
}
