import type { ReportDefinition, ReportQuery } from "./types";

const PAGE_SIZE = 20;

// Burada her rapor için yalnız katalogda izin verilen filtreleri URL'den kabul ediyorum.
export function parseReportQuery(params: Record<string, string | string[] | undefined>, report: ReportDefinition): ReportQuery {
  const from = report.filters.date ? isoDate(params.from) : "";
  const requestedTo = report.filters.date ? isoDate(params.to) : "";
  return {
    pageNumber: positiveInteger(params.pageNumber, 1),
    pageSize: PAGE_SIZE,
    search: report.filters.search ? text(params.search).slice(0, 200) : "",
    from,
    to: from && requestedTo && requestedTo < from ? "" : requestedTo,
    id: report.filters.id ? uuid(params.id) : "",
    hasSalesInvoice: report.filters.invoiceStatus ? invoiceStatus(params.hasSalesInvoice) : "all",
    scopeId: report.scope ? uuid(params.scopeId) : "",
  };
}

// Burada rapor filtresi ve sayfa durumunu paylaşılabilir, kanonik bir admin URL'sine dönüştürüyorum.
export function buildReportHref(report: ReportDefinition, query: ReportQuery, pageNumber = query.pageNumber): string {
  const params = new URLSearchParams();
  if (report.scope && query.scopeId) params.set("scopeId", query.scopeId);
  if (report.filters.search && query.search) params.set("search", query.search);
  if (report.filters.date && query.from) params.set("from", query.from);
  if (report.filters.date && query.to) params.set("to", query.to);
  if (report.filters.id && query.id) params.set("id", query.id);
  if (report.filters.invoiceStatus && query.hasSalesInvoice !== "all") params.set("hasSalesInvoice", query.hasSalesInvoice);
  if (pageNumber > 1) params.set("pageNumber", String(pageNumber));
  const encoded = params.toString();
  return encoded ? `/accounting/reports/${report.slug}?${encoded}` : `/accounting/reports/${report.slug}`;
}

export function canonicalReportPage(requested: number, totalPages: number): number | null {
  const canonical = totalPages === 0 ? 1 : Math.min(requested, totalPages);
  return canonical === requested ? null : canonical;
}

function text(value: string | string[] | undefined): string {
  return (Array.isArray(value) ? value[0] : value || "").trim();
}

function uuid(value: string | string[] | undefined): string {
  const candidate = text(value);
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(candidate) ? candidate : "";
}

function isoDate(value: string | string[] | undefined): string {
  const candidate = text(value);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(candidate)) return "";
  const parsed = new Date(`${candidate}T00:00:00.000Z`);
  return Number.isNaN(parsed.getTime()) || parsed.toISOString().slice(0, 10) !== candidate ? "" : candidate;
}

function positiveInteger(value: string | string[] | undefined, fallback: number): number {
  const parsed = Number(text(value));
  return Number.isInteger(parsed) && parsed > 0 ? Math.min(parsed, 10_000) : fallback;
}

function invoiceStatus(value: string | string[] | undefined): ReportQuery["hasSalesInvoice"] {
  const candidate = text(value);
  return candidate === "yes" || candidate === "no" ? candidate : "all";
}
