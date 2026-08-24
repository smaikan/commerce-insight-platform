import Link from "next/link";
import { formatAccountingDate, formatAccountingMoney } from "@/modules/accounting/core/presentation";
import { invoiceStatusClass, invoiceStatusLabel } from "../presentation";
import type { PurchaseInvoicePage } from "../types";

export function PurchaseInvoiceTable({ page }: { page: PurchaseInvoicePage }) {
  if (!page.items.length) {
    return <div className="px-5 py-14 text-center"><h2 className="text-base font-semibold">Henüz alış faturası bulunmuyor</h2><p className="mt-2 text-sm text-muted">İlk tedarikçi belgesini taslak olarak oluşturarak başlayın.</p></div>;
  }

  // Burada alış faturalarını ürün kartı yerine belge numarası ve finansal durum odaklı sicil tablosunda gösteriyorum.
  return (
    <div role="region" aria-label="Alış faturası sicili; yatay kaydırılabilir" tabIndex={0} className="overflow-x-auto bg-surface outline-none focus:ring-2 focus:ring-inset focus:ring-focus/30">
      <table className="w-full min-w-[860px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr><th scope="col" className="sticky left-0 z-10 w-56 bg-surface-subtle px-4 py-2.5">Belge</th><th scope="col" className="px-3 py-2.5">Tedarikçi</th><th scope="col" className="px-3 py-2.5">Fatura tarihi</th><th scope="col" className="px-3 py-2.5">Durum</th><th scope="col" className="px-4 py-2.5 text-right">Genel toplam</th></tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((invoice) => (
            <tr key={invoice.id} className="hover:bg-primary-soft/20">
              <td className="sticky left-0 bg-surface px-4 py-3"><Link href={`/accounting/purchase-invoices/${encodeURIComponent(invoice.id)}`} className="font-semibold text-foreground hover:text-primary">{invoice.invoiceNumber}</Link><span className="mt-0.5 block font-mono text-[11px] text-muted">{invoice.id.slice(0, 8)}</span></td>
              <td className="max-w-xs px-3 py-3"><Link href={`/accounting/current-accounts/${encodeURIComponent(invoice.currentAccountId)}`} className="font-medium hover:text-primary">{invoice.currentAccountName}</Link></td>
              <td className="px-3 py-3 text-muted">{formatAccountingDate(invoice.invoiceDate)}</td>
              <td className="px-3 py-3"><span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-bold ${invoiceStatusClass(invoice.status)}`}>{invoiceStatusLabel(invoice.status)}</span></td>
              <td className="px-4 py-3 text-right font-semibold tabular-nums">{formatAccountingMoney(invoice.grandTotalIncludingVat, invoice.currencyCode)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
