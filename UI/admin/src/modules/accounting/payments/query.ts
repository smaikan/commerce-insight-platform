import type { PaymentListQuery, PaymentSetup } from "./types";

const PAGE_SIZE = 20;
export function parsePaymentListQuery(params: Record<string, string | string[] | undefined>): PaymentListQuery {
  const raw = Array.isArray(params.pageNumber) ? params.pageNumber[0] : params.pageNumber;
  const page = Number(raw);
  return { pageNumber: Number.isInteger(page) && page > 0 ? Math.min(page, 10_000) : 1, pageSize: PAGE_SIZE };
}
export function buildPaymentListHref(query: PaymentListQuery, pageNumber = query.pageNumber): string {
  return pageNumber <= 1 ? "/accounting/payments" : `/accounting/payments?pageNumber=${pageNumber}`;
}
export function canonicalPaymentPage(requested: number, totalPages: number): number | null {
  const canonical = totalPages === 0 ? 1 : Math.min(requested, totalPages);
  return canonical === requested ? null : canonical;
}
export function parsePaymentSetup(params: Record<string, string | string[] | undefined>): PaymentSetup | null {
  const rawType = Array.isArray(params.type) ? params.type[0] : params.type;
  const currentAccountId = Array.isArray(params.currentAccountId) ? params.currentAccountId[0] : params.currentAccountId;
  const type = Number(rawType);
  return (type === 1 || type === 2) && Boolean(currentAccountId) ? { type, currentAccountId: currentAccountId! } : null;
}
