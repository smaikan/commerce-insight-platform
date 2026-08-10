import type { BrandListQuery } from "@/modules/brands/types";

const DEFAULT_PAGE_SIZE = 20;

// Burada marka listesinin URL değerlerini API sayfalama sınırları içinde güvenli sayılara dönüştürüyorum.
export function parseBrandListQuery(params: Record<string, string | string[] | undefined>): BrandListQuery {
  return {
    pageNumber: boundedInteger(single(params.pageNumber), 1, 10_000, 1),
    pageSize: boundedInteger(single(params.pageSize), 1, 100, DEFAULT_PAGE_SIZE),
  };
}

// Burada marka sayfalama bağlantılarında mevcut sayfa boyutunu koruyorum.
export function buildBrandListHref(query: BrandListQuery, pageNumber = query.pageNumber): string {
  const params = new URLSearchParams();
  if (pageNumber > 1) params.set("pageNumber", String(pageNumber));
  if (query.pageSize !== DEFAULT_PAGE_SIZE) params.set("pageSize", String(query.pageSize));
  return params.size ? `/brands?${params.toString()}` : "/brands";
}

// Burada tekrarlı URL parametrelerinden ilk değeri seçiyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada sayısal URL değerlerini tanımlı alt ve üst sınırlara bağlıyorum.
function boundedInteger(value: string | undefined, minimum: number, maximum: number, fallback: number): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= minimum && parsed <= maximum ? parsed : fallback;
}
