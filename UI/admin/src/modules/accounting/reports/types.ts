import type { AccountingReportPage, AccountingReportRow } from "@/modules/accounting/core/types";

export type ReportGroup = "Satış ve alış" | "Stok ve maliyet" | "Kârlılık" | "Cari ve nakit" | "Vergi";
export type ReportField = keyof AccountingReportRow;
export type ReportColumnKind = "text" | "date" | "money" | "number" | "percent" | "boolean";

export type ReportColumn = { field: ReportField; label: string; kind: ReportColumnKind; align?: "left" | "right" };
export type ReportFilters = { search?: boolean; date?: boolean; id?: boolean; idLabel?: string; invoiceStatus?: boolean };
export type ReportDefinition = {
  slug: string;
  title: string;
  shortTitle: string;
  description: string;
  group: ReportGroup;
  endpoint: string;
  scope?: { label: string; placeholder: string };
  filters: ReportFilters;
  columns: ReportColumn[];
};

export type ReportQuery = {
  pageNumber: number;
  pageSize: number;
  search: string;
  from: string;
  to: string;
  id: string;
  hasSalesInvoice: "all" | "yes" | "no";
  scopeId: string;
};

export type { AccountingReportPage, AccountingReportRow };
