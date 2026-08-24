import type { SalesListQuery } from "./types";

const PAGE_SIZE = 20;

export function parseSalesListQuery(params: Record<string, string | string[] | undefined>): SalesListQuery {
  const value = Array.isArray(params.pageNumber) ? params.pageNumber[0] : params.pageNumber;
  const parsed = Number(value);
  return { pageNumber: Number.isInteger(parsed) && parsed > 0 ? Math.min(parsed, 10_000) : 1, pageSize: PAGE_SIZE };
}

export function buildSalesOrderListHref(query: SalesListQuery, pageNumber = query.pageNumber): string {
  return pageNumber <= 1 ? "/accounting/sales-orders" : `/accounting/sales-orders?pageNumber=${pageNumber}`;
}

export function buildSalesInvoiceListHref(query: SalesListQuery, pageNumber = query.pageNumber): string {
  return pageNumber <= 1 ? "/accounting/sales-invoices" : `/accounting/sales-invoices?pageNumber=${pageNumber}`;
}

export function canonicalSalesPage(requested: number, totalPages: number): number | null {
  const canonical = totalPages === 0 ? 1 : Math.min(requested, totalPages);
  return canonical === requested ? null : canonical;
}
