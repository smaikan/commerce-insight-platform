import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getAccountingReportPage } from "@/modules/accounting/reports/api";
import { getAccountingReport } from "@/modules/accounting/reports/catalog";
import { AccountingReportTable } from "@/modules/accounting/reports/components/accounting-report-table";
import { ReportFilters } from "@/modules/accounting/reports/components/report-filters";
import { ReportPagination } from "@/modules/accounting/reports/components/report-pagination";
import { buildReportHref, canonicalReportPage, parseReportQuery } from "@/modules/accounting/reports/query";

type Props = { params: Promise<{ report: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> };

// Burada yalnız katalogda bulunan sluglar için rapora özgü tarayıcı başlığı üretiyorum.
export async function generateMetadata({ params }: Pick<Props, "params">): Promise<Metadata> {
  const report = getAccountingReport((await params).report);
  return { title: report?.title || "Muhasebe Raporu" };
}

// Burada kimlik kapsamı isteyen raporu, geçerli kapsam seçilmeden API'ye göndermiyorum.
export default async function AccountingReportPage({ params, searchParams }: Props) {
  const report = getAccountingReport((await params).report);
  if (!report) notFound();
  const query = parseReportQuery(await searchParams, report);
  const session = await requireAdminPageSession(`/accounting/reports/${report.slug}`);
  if (report.scope && !query.scopeId) return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title={report.title} description={report.description} backHref="/accounting/reports" /><ReportFilters report={report} query={query} /><section className="rounded-xl border border-dashed border-border-strong bg-surface px-6 py-14 text-center"><h2 className="font-semibold">Rapor kapsamını seçin</h2><p className="mx-auto mt-2 max-w-xl text-sm leading-6 text-muted">{report.scope.label} alanına geçerli bir UUID girip raporu getirin. Eksik veya geçersiz kimlik API’ye gönderilmez.</p></section></div>;
  let page;
  try {
    page = await getAccountingReportPage(report, query, session);
  } catch (error) {
    if (error instanceof ApiError) return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title={report.title} description={report.description} backHref="/accounting/reports" /><ReportFilters report={report} query={query} /><AccountingLoadProblem problem={error.problem} retryHref={buildReportHref(report, query)} /></div>;
    throw error;
  }
  const canonical = canonicalReportPage(query.pageNumber, page.totalPages);
  if (canonical) redirect(buildReportHref(report, query, canonical));
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title={report.title} description={report.description} backHref="/accounting/reports" actions={<Link href="/accounting/reports" className="inline-flex min-h-10 cursor-pointer items-center rounded-lg border border-border-strong bg-surface px-4 text-sm font-semibold hover:bg-surface-subtle">Rapor dizini</Link>} /><ReportFilters report={report} query={query} /><AccountingReportTable report={report} page={page} /><ReportPagination report={report} query={query} page={page} /></div>;
}
