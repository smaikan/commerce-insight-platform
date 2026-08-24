import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { AccountingReportPage, ReportDefinition, ReportQuery } from "./types";

// Burada katalog allowlist'indeki endpointi rapora özgü filtrelerle çağırıyorum.
export function getAccountingReportPage(report: ReportDefinition, query: ReportQuery, session: AdminSession): Promise<AccountingReportPage> {
  const endpoint = report.endpoint.replace("{scopeId}", encodeURIComponent(query.scopeId));
  const params = new URLSearchParams({ PageNumber: String(query.pageNumber), PageSize: String(query.pageSize) });
  if (report.filters.search && query.search) params.set("Search", query.search);
  if (report.filters.date && query.from) params.set("From", `${query.from}T00:00:00.000Z`);
  if (report.filters.date && query.to) params.set("To", `${query.to}T23:59:59.999Z`);
  if (report.filters.id && query.id) params.set("Id", query.id);
  if (report.filters.invoiceStatus && query.hasSalesInvoice !== "all") params.set("HasSalesInvoice", String(query.hasSalesInvoice === "yes"));
  return apiRequest(`${endpoint}?${params}`, { accessToken: session.accessToken });
}
