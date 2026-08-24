import Link from "next/link";
import type { ReportDefinition, ReportQuery } from "../types";

// Burada yalnız raporun veri semantiğinde karşılığı olan filtreleri görünür kılıyorum.
export function ReportFilters({ report, query }: { report: ReportDefinition; query: ReportQuery }) {
  const hasFilters = Boolean(report.scope || report.filters.search || report.filters.date || report.filters.id || report.filters.invoiceStatus);
  if (!hasFilters) return null;
  return (
    <form className="mb-4 rounded-xl border border-border bg-surface p-4">
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        {report.scope ? <label className="text-sm font-medium">{report.scope.label} *<input name="scopeId" required placeholder={report.scope.placeholder} defaultValue={query.scopeId} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong px-3 font-mono text-sm" /></label> : null}
        {report.filters.search ? <label className="text-sm font-medium">Metin ara<input name="search" type="search" maxLength={200} defaultValue={query.search} placeholder="Belge, ad veya referans" className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong px-3 text-sm" /></label> : null}
        {report.filters.date ? <><label className="text-sm font-medium">Başlangıç tarihi<input name="from" type="date" defaultValue={query.from} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong px-3 text-sm" /></label><label className="text-sm font-medium">Bitiş tarihi<input name="to" type="date" min={query.from || undefined} defaultValue={query.to} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong px-3 text-sm" /></label></> : null}
        {report.filters.id ? <label className="text-sm font-medium">{report.filters.idLabel || "Kayıt kimliği"}<input name="id" placeholder="UUID" defaultValue={query.id} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong px-3 font-mono text-sm" /></label> : null}
        {report.filters.invoiceStatus ? <label className="text-sm font-medium">Fatura durumu<select name="hasSalesInvoice" defaultValue={query.hasSalesInvoice} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-white px-3 text-sm"><option value="all">Tümü</option><option value="yes">Faturalı</option><option value="no">Faturasız</option></select></label> : null}
      </div>
      <div className="mt-4 flex flex-wrap justify-end gap-2">
        <Link href={`/accounting/reports/${report.slug}`} className="inline-flex min-h-10 cursor-pointer items-center rounded-lg border border-border-strong px-4 text-sm font-semibold hover:bg-surface-subtle">Filtreleri temizle</Link>
        <button type="submit" className="min-h-10 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Raporu getir</button>
      </div>
    </form>
  );
}
