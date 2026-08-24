import Link from "next/link";
import { accountingReportCatalog, accountingReportGroups } from "../catalog";

// Burada raporları ürün kartı estetiği yerine finans çalışma konularına ayrılmış bir rapor dizini olarak sunuyorum.
export function ReportCatalog() {
  return (
    <div className="space-y-5">
      {accountingReportGroups.map((group, groupIndex) => (
        <section key={group} className="overflow-hidden rounded-xl border border-border bg-surface">
          <div className="flex items-center gap-3 border-b border-border bg-surface-subtle/70 px-5 py-3">
            <span aria-hidden="true" className="flex size-7 items-center justify-center rounded-md bg-slate-900 text-xs font-bold text-white">{String(groupIndex + 1).padStart(2, "0")}</span>
            <h2 className="font-semibold">{group}</h2>
          </div>
          <div className="grid md:grid-cols-2">
            {accountingReportCatalog.filter((report) => report.group === group).map((report, index) => (
              <Link key={report.slug} href={`/accounting/reports/${report.slug}`} className={`group flex min-h-24 items-start justify-between gap-4 px-5 py-4 hover:bg-primary-soft/20 ${index > 0 ? "border-t border-border/80 md:border-t-0" : ""} ${index % 2 === 1 ? "md:border-l md:border-border/80" : ""} ${index > 1 ? "md:border-t" : ""}`}>
                <span><strong className="block text-sm group-hover:text-primary">{report.shortTitle}</strong><span className="mt-1 block text-sm leading-5 text-muted">{report.description}</span>{report.scope ? <span className="mt-2 inline-flex rounded-md bg-surface-subtle px-2 py-0.5 text-[11px] font-semibold text-muted">Kimlik ile çalışır</span> : null}</span>
                <span aria-hidden="true" className="mt-1 text-primary">→</span>
              </Link>
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
