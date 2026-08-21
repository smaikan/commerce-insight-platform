import type {
  ContactMessageListQuery,
  ContactMessageStatus,
  ContactMessageSubject,
} from "@/modules/contact-messages/types";

const pageSizes = [10, 20, 50, 100] as const;
const statuses = [0, 1, 2, 3, 4, 5] as const;
const subjects = [0, 1, 2, 3, 4, 5] as const;
const datePattern = /^\d{4}-\d{2}-\d{2}$/;
const publicAdminIdPattern = /^U[0-9A-Z]{5,7}$/;

// Burada URL parametrelerini yalnız belgelenmiş iletişim filtreleri ve güvenli sayfalama sınırlarıyla parse ediyorum.
export function parseContactMessageListQuery(
  params: Record<string, string | string[] | undefined>,
): ContactMessageListQuery {
  const pageNumber = boundedInteger(single(params.pageNumber), 1, 10_000, 1);
  const requestedPageSize = boundedInteger(single(params.pageSize), 1, 100, 20);
  const pageSize = pageSizes.includes(requestedPageSize as (typeof pageSizes)[number])
    ? requestedPageSize
    : 20;
  const search = single(params.search)?.trim() || undefined;
  const status = numericEnum(single(params.status), statuses) as ContactMessageStatus | undefined;
  const subject = numericEnum(single(params.subject), subjects) as ContactMessageSubject | undefined;
  const assignedAdminUserIdValue = single(params.assignedAdminUserId)?.trim().toUpperCase();
  const assignedAdminUserId = assignedAdminUserIdValue && publicAdminIdPattern.test(assignedAdminUserIdValue)
    ? assignedAdminUserIdValue
    : undefined;
  const createdFromUtc = validDate(single(params.createdFromUtc));
  const createdToUtc = validDate(single(params.createdToUtc));
  const dateError = createdFromUtc && createdToUtc && createdFromUtc > createdToUtc
    ? "Başlangıç tarihi bitiş tarihinden sonra olamaz."
    : undefined;

  return {
    pageNumber,
    pageSize,
    search,
    status,
    subject,
    assignedAdminUserId,
    createdFromUtc,
    createdToUtc,
    createdFromApiUtc: !dateError && createdFromUtc ? `${createdFromUtc}T00:00:00.000Z` : undefined,
    createdToApiUtc: !dateError && createdToUtc ? `${createdToUtc}T23:59:59.999Z` : undefined,
    dateError,
  };
}

// Burada filtre varlığını boş sonuç durumu ve temizleme aksiyonu için tek yerde belirliyorum.
export function hasContactMessageFilters(query: ContactMessageListQuery): boolean {
  return Boolean(
    query.search ||
    query.status !== undefined ||
    query.subject !== undefined ||
    query.assignedAdminUserId ||
    query.createdFromUtc ||
    query.createdToUtc,
  );
}

// Burada liste ve detay dönüş bağlantılarında yalnız belgelenmiş filtreleri koruyorum.
export function buildContactMessageListHref(query: ContactMessageListQuery, pageNumber = query.pageNumber): string {
  const params = new URLSearchParams();
  if (pageNumber > 1) params.set("pageNumber", String(pageNumber));
  if (query.pageSize !== 20) params.set("pageSize", String(query.pageSize));
  if (query.search) params.set("search", query.search);
  if (query.status !== undefined) params.set("status", String(query.status));
  if (query.subject !== undefined) params.set("subject", String(query.subject));
  if (query.assignedAdminUserId) params.set("assignedAdminUserId", query.assignedAdminUserId);
  if (query.createdFromUtc) params.set("createdFromUtc", query.createdFromUtc);
  if (query.createdToUtc) params.set("createdToUtc", query.createdToUtc);
  const queryString = params.toString();
  return queryString ? `/contact-messages?${queryString}` : "/contact-messages";
}

// Burada liste satırından detaya giderken aynı belgelenmiş filtre parametrelerini taşıyorum.
export function buildContactMessageDetailHref(messageId: string, query: ContactMessageListQuery): string {
  const listHref = buildContactMessageListHref(query);
  const queryString = listHref.includes("?") ? listHref.slice(listHref.indexOf("?") + 1) : "";
  const path = `/contact-messages/${encodeURIComponent(messageId)}`;
  return queryString ? `${path}?${queryString}` : path;
}

// Burada yinelenebilen Next.js search param değerlerinden yalnız ilk metni alıyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

// Burada sayısal URL değerini belgelenmiş alt ve üst sınırlar içinde tutuyorum.
function boundedInteger(value: string | undefined, min: number, max: number, fallback: number): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
}

// Burada numeric enum filtresini yalnız yayımlanmış allowlist değerlerinden kabul ediyorum.
function numericEnum(value: string | undefined, allowed: readonly number[]): number | undefined {
  if (value === undefined || value === "") return undefined;
  const parsed = Number(value);
  return allowed.includes(parsed) ? parsed : undefined;
}

// Burada HTML tarih değerini gerçekten var olan bir UTC takvim günü olduğunda kabul ediyorum.
function validDate(value: string | undefined): string | undefined {
  if (!value || !datePattern.test(value)) return undefined;
  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));
  return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day
    ? value
    : undefined;
}
