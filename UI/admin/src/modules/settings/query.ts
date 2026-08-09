import type { SettingsListQuery } from "@/modules/settings/types";

const DEFAULT_PAGE_SIZE = 20;

// Burada bilinmeyen veya sınır dışı URL değerlerini güvenli liste varsayılanlarına çekiyorum.
export function parseSettingsListQuery(params: Record<string, string | string[] | undefined>): SettingsListQuery {
  return {
    pageNumber: parseInteger(params.page, 1, 1, 10_000),
    pageSize: parseInteger(params.pageSize, DEFAULT_PAGE_SIZE, 1, 100),
  };
}

// Burada ayar listelerinde sayfa değiştirilirken mevcut sayfa boyutunu koruyorum.
export function settingsListHref(basePath: string, query: SettingsListQuery, pageNumber: number): string {
  const params = new URLSearchParams({ page: String(pageNumber), pageSize: String(query.pageSize) });
  return `${basePath}?${params.toString()}`;
}

// Burada URL'den gelen tekil sayısal değeri doğrulayıp sınırlandırıyorum.
function parseInteger(value: string | string[] | undefined, fallback: number, minimum: number, maximum: number): number {
  const raw = Array.isArray(value) ? value[0] : value;
  const parsed = Number.parseInt(raw ?? "", 10);
  return Number.isFinite(parsed) && parsed >= minimum && parsed <= maximum ? parsed : fallback;
}
