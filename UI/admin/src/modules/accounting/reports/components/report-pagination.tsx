import Link from "next/link";
import { buildReportHref } from "../query";
import type { AccountingReportPage, ReportDefinition, ReportQuery } from "../types";

// Burada yalnız API totalCount ve totalPages değerleriyle rapor sayfaları arasında geziniyorum.
export function ReportPagination({ report, query, page }: { report: ReportDefinition; query: ReportQuery; page: AccountingReportPage }) {
  if (page.totalPages <= 1) return null;
  return <nav aria-label="Rapor sayfaları" className="mt-4 flex items-center justify-between gap-3"><Link aria-disabled={!page.hasPreviousPage} tabIndex={page.hasPreviousPage ? undefined : -1} href={page.hasPreviousPage ? buildReportHref(report, query, page.pageNumber - 1) : "#"} className={`inline-flex min-h-10 items-center rounded-lg border border-border-strong px-4 text-sm font-semibold ${page.hasPreviousPage ? "cursor-pointer hover:bg-surface" : "pointer-events-none opacity-50"}`}>Önceki</Link><span className="text-sm text-muted"><strong className="text-foreground">{page.pageNumber}</strong> / {page.totalPages}</span><Link aria-disabled={!page.hasNextPage} tabIndex={page.hasNextPage ? undefined : -1} href={page.hasNextPage ? buildReportHref(report, query, page.pageNumber + 1) : "#"} className={`inline-flex min-h-10 items-center rounded-lg border border-border-strong px-4 text-sm font-semibold ${page.hasNextPage ? "cursor-pointer hover:bg-surface" : "pointer-events-none opacity-50"}`}>Sonraki</Link></nav>;
}
