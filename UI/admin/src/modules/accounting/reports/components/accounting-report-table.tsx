import { formatAccountingDate, formatAccountingMoney } from "@/modules/accounting/core/presentation";
import type { AccountingReportPage, AccountingReportRow, ReportColumn, ReportDefinition } from "../types";

// Burada raporun kendi kolon sözlüğünü kullanarak finansal anlamları birbirine karıştırmadan hücreleri biçimliyorum.
export function AccountingReportTable({ report, page }: { report: ReportDefinition; page: AccountingReportPage }) {
  if (!page.items.length) return <section className="rounded-xl border border-border bg-surface px-5 py-14 text-center"><h2 className="font-semibold">Bu koşullarda kayıt bulunamadı</h2><p className="mt-2 text-sm text-muted">Filtreleri değiştirin veya kaynak muhasebe kayıtlarını kontrol edin.</p></section>;
  return (
    <section className="overflow-hidden rounded-xl border border-border bg-surface">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border px-4 py-3">
        <div><h2 className="font-semibold">Rapor kayıtları</h2><p className="mt-0.5 text-xs text-muted">Geniş tabloyu yatay kaydırabilirsiniz. Sayfa toplamı finansal toplam değildir.</p></div>
        <span className="rounded-md bg-surface-subtle px-2.5 py-1 text-xs font-semibold tabular-nums">{page.totalCount} kayıt</span>
      </div>
      <div role="region" aria-label={`${report.title}; yatay kaydırılabilir`} tabIndex={0} className="overflow-x-auto outline-none focus:ring-2 focus:ring-inset focus:ring-focus/30">
        <table className="w-full min-w-[880px] border-collapse text-left text-sm">
          <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted"><tr>{report.columns.map((column) => <th key={`${column.field}-${column.label}`} scope="col" className={`whitespace-nowrap px-4 py-2.5 ${column.align === "right" ? "text-right" : "text-left"}`}>{column.label}</th>)}</tr></thead>
          <tbody className="divide-y divide-border/80">{page.items.map((row, index) => <tr key={`${row.id}-${row.relatedId || "none"}-${index}`} className="hover:bg-primary-soft/20">{report.columns.map((column) => <td key={`${column.field}-${column.label}`} className={`whitespace-nowrap px-4 py-3 ${column.align === "right" ? "text-right tabular-nums" : ""}`}>{formatCell(row, column)}</td>)}</tr>)}</tbody>
        </table>
      </div>
    </section>
  );
}

// Burada değer biçimini kolonun rapora özel semantiğinden türetip ham alan adından bağımsızlaştırıyorum.
function formatCell(row: AccountingReportRow, column: ReportColumn): string {
  const value = row[column.field];
  if (value == null || value === "") return "—";
  if (column.kind === "date") return formatAccountingDate(String(value));
  if (column.kind === "money") return formatAccountingMoney(Number(value), row.currencyCode);
  if (column.kind === "number") return new Intl.NumberFormat("tr-TR").format(Number(value));
  if (column.kind === "percent") return `${new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 2 }).format(Number(value))}%`;
  if (column.kind === "boolean") return value ? "Evet" : "Hayır";
  return String(value);
}
