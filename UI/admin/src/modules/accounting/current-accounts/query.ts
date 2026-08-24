import type { CurrentAccountListQuery, CurrentAccountStatementQuery } from "@/modules/accounting/current-accounts/types";

const DEFAULT_PAGE_SIZE = 20;

export function parseCurrentAccountListQuery(params: Record<string, string | string[] | undefined>): CurrentAccountListQuery {
  return {
    pageNumber: bounded(single(params.pageNumber), 1, 10_000, 1),
    pageSize: bounded(single(params.pageSize), 1, 100, DEFAULT_PAGE_SIZE),
  };
}

export function buildCurrentAccountListHref(query: CurrentAccountListQuery, pageNumber = query.pageNumber): string {
  const params = new URLSearchParams();
  if (pageNumber > 1) params.set("pageNumber", String(pageNumber));
  if (query.pageSize !== DEFAULT_PAGE_SIZE) params.set("pageSize", String(query.pageSize));
  return params.size ? `/accounting/current-accounts?${params}` : "/accounting/current-accounts";
}

export function parseCurrentAccountStatementQuery(params: Record<string, string | string[] | undefined>): CurrentAccountStatementQuery {
  return {
    statementPageNumber: bounded(single(params.statementPageNumber), 1, 10_000, 1),
    statementPageSize: bounded(single(params.statementPageSize), 1, 100, DEFAULT_PAGE_SIZE),
  };
}

export function buildCurrentAccountStatementHref(id: string, query: CurrentAccountStatementQuery, pageNumber = query.statementPageNumber): string {
  const params = new URLSearchParams();
  if (pageNumber > 1) params.set("statementPageNumber", String(pageNumber));
  if (query.statementPageSize !== DEFAULT_PAGE_SIZE) params.set("statementPageSize", String(query.statementPageSize));
  const pathname = `/accounting/current-accounts/${encodeURIComponent(id)}`;
  return params.size ? `${pathname}?${params}` : pathname;
}

export function canonicalPageNumber(requestedPage: number, totalPages: number): number | null {
  const lastPage = Math.max(totalPages, 1);
  return requestedPage > lastPage ? lastPage : null;
}

function single(value: string | string[] | undefined): string | undefined { return Array.isArray(value) ? value[0] : value; }
function bounded(value: string | undefined, min: number, max: number, fallback: number): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
}
