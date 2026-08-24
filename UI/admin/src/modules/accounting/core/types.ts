import type { components } from "@/generated/api";
import type { PagedResult } from "@/lib/api/pagination";

export type AccountingReportRow = components["schemas"]["AccountingReportRowDto"];
export type AccountingReportPage = PagedResult<AccountingReportRow>;

export type AccountingQueue = {
  key: "overdue-receivables" | "overdue-debts" | "uncosted-stock" | "partially-costed-stock";
  title: string;
  description: string;
  href: string;
  totalCount: number | null;
  unavailable: boolean;
};
