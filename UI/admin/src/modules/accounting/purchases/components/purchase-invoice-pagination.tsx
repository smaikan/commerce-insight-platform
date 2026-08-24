import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { buildPurchaseInvoiceListHref } from "../query";
import type { PurchaseInvoiceListQuery, PurchaseInvoicePage } from "../types";

export function PurchaseInvoicePagination({ page, query }: { page: PurchaseInvoicePage; query: PurchaseInvoiceListQuery }) {
  return <AdminPagination action="/accounting/purchase-invoices" ariaLabel="Alış faturası sayfalama" buildHref={(pageNumber) => buildPurchaseInvoiceListHref(query, pageNumber)} itemLabel="alış faturası" pageNumber={page.pageNumber} pageSize={page.pageSize} totalCount={page.totalCount} totalPages={page.totalPages} />;
}
