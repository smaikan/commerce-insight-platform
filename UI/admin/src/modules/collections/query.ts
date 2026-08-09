import type { CollectionListQuery } from "@/modules/collections/types";

// Burada koleksiyon listesinin URL değerlerini API sınırları içinde güvenli sayılara dönüştürüyorum.
export function parseCollectionListQuery(params: Record<string, string | string[] | undefined>): CollectionListQuery {
  return {
    pageNumber: boundedInteger(single(params.pageNumber), 1, 10_000, 1),
    pageSize: boundedInteger(single(params.pageSize), 1, 100, 20),
  };
}

// Burada koleksiyon sayfalama bağlantılarında mevcut sayfa boyutunu koruyorum.
export function buildCollectionListHref(query: CollectionListQuery, pageNumber = query.pageNumber): string {
  const params = new URLSearchParams();
  if (pageNumber > 1) params.set("pageNumber", String(pageNumber));
  if (query.pageSize !== 20) params.set("pageSize", String(query.pageSize));
  return params.size ? `/collections?${params.toString()}` : "/collections";
}

// Burada tekrarlı URL parametrelerinden ilk değeri seçiyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada sayısal URL değerlerini tanımlı alt ve üst sınırlara bağlıyorum.
function boundedInteger(value: string | undefined, min: number, max: number, fallback: number): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
}
