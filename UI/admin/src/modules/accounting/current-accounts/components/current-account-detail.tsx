import Link from "next/link";
import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { formatAccountingDate, formatAccountingMoney } from "@/modules/accounting/core/presentation";
import type { AccountingReportPage } from "@/modules/accounting/core/types";
import { currentAccountTypeClass, currentAccountTypeLabel } from "@/modules/accounting/current-accounts/presentation";
import { buildCurrentAccountStatementHref } from "@/modules/accounting/current-accounts/query";
import type { CurrentAccount, CurrentAccountStatementQuery } from "@/modules/accounting/current-accounts/types";

export function CurrentAccountDetail({ account, statement, statementQuery }: { account: CurrentAccount; statement: AccountingReportPage; statementQuery: CurrentAccountStatementQuery }) {
  const detailPath = `/accounting/current-accounts/${encodeURIComponent(account.id)}`;
  return (
    <div className="grid gap-5 xl:grid-cols-[22rem_minmax(0,1fr)]">
      <aside className="space-y-4">
        <section className="rounded-xl border border-border bg-surface p-5">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="font-mono text-xs text-muted">{account.code}</p>
              <h2 className="mt-1 text-lg font-semibold">{account.name}</h2>
            </div>
            <span className={`rounded-md border px-2 py-1 text-xs font-bold ${account.isActive ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-border bg-surface-subtle text-muted"}`}>{account.isActive ? "Aktif" : "Pasif"}</span>
          </div>
          <span className={`mt-3 inline-flex rounded-md border px-2 py-1 text-xs font-bold ${currentAccountTypeClass(account.type)}`}>{currentAccountTypeLabel(account.type)}</span>
        </section>
        <Info title="Vergi ve iletişim" rows={[["Ticari unvan", account.tradeName], ["Vergi numarası", account.taxNumber || account.nationalIdentityNumber], ["Vergi dairesi", account.taxOffice], ["E-posta", account.email], ["Telefon", account.phoneNumber]]} />
        <Info title="Adres" rows={[["Adres", account.addressLine], ["Mahalle", account.neighborhood], ["İlçe", account.district], ["Şehir", account.city], ["Ülke", account.country], ["Posta kodu", account.postalCode]]} />
        <Link href={`${detailPath}/edit`} className="inline-flex min-h-11 w-full items-center justify-center rounded-lg border border-border-strong bg-surface px-4 text-sm font-semibold hover:bg-surface-subtle">Cari hesabı düzenle</Link>
      </aside>

      <section className="overflow-hidden rounded-xl border border-border bg-surface" aria-labelledby="statement-title">
        <div className="flex items-start justify-between gap-4 border-b border-border px-4 py-4">
          <div>
            <h2 id="statement-title" className="font-semibold">Cari ekstre</h2>
            <p className="mt-1 text-sm text-muted">Post edilmiş hareketlerin borç, alacak ve tahsis/reversal sonrası açık tutar görünümü.</p>
            {statement.items.length ? <p className="mt-2 text-xs font-medium text-muted sm:hidden">Tüm kolonlar için tabloyu yatay kaydırın.</p> : null}
          </div>
          <span className="shrink-0 text-xs text-muted">{statement.totalCount} hareket</span>
        </div>
        {statement.items.length ? (
          <>
            <div role="region" aria-label="Cari ekstre tablosu; yatay kaydırılabilir" tabIndex={0} className="overflow-x-auto outline-none focus:ring-2 focus:ring-inset focus:ring-focus/30">
              <table className="w-full min-w-[720px] text-left text-sm">
                <thead className="border-b border-border bg-surface-subtle/80 text-xs text-muted">
                  <tr>
                    <th scope="col" className="sticky left-0 z-10 w-32 bg-surface-subtle px-4 py-2.5">İşlem</th>
                    <th scope="col" className="px-3 py-2.5">Tarih / vade</th>
                    <th scope="col" className="px-3 py-2.5 text-right">Borç</th>
                    <th scope="col" className="px-3 py-2.5 text-right">Alacak</th>
                    <th scope="col" className="px-4 py-2.5 text-right">Açık tutar</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {statement.items.map((row) => (
                    <tr key={row.id}>
                      <td title={row.id} className="sticky left-0 w-32 max-w-32 bg-surface px-4 py-3 font-mono text-xs text-muted"><span aria-hidden="true">{row.id.slice(0, 8)}…</span><span className="sr-only">{row.id}</span></td>
                      <td className="px-3 py-3"><span className="block">{formatAccountingDate(row.date)}</span><span className="text-xs text-muted">Vade: {formatAccountingDate(row.dueDate)}</span></td>
                      <td className="px-3 py-3 text-right tabular-nums">{formatAccountingMoney(row.amount, row.currencyCode)}</td>
                      <td className="px-3 py-3 text-right tabular-nums">{formatAccountingMoney(row.secondaryAmount, row.currencyCode)}</td>
                      <td className="px-4 py-3 text-right font-semibold tabular-nums">{formatAccountingMoney(row.tertiaryAmount, row.currencyCode)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <AdminPagination
              action={detailPath}
              ariaLabel="Cari ekstre sayfalama"
              buildHref={(pageNumber) => buildCurrentAccountStatementHref(account.id, statementQuery, pageNumber)}
              hiddenFields={statementQuery.statementPageSize !== 20 ? [{ name: "statementPageSize", value: statementQuery.statementPageSize }] : []}
              itemLabel="hareket"
              pageNumber={statement.pageNumber}
              pageParam="statementPageNumber"
              pageSize={statement.pageSize}
              totalCount={statement.totalCount}
              totalPages={statement.totalPages}
            />
          </>
        ) : (
          <div className="px-5 py-14 text-center"><h3 className="font-semibold">Henüz cari hareket yok</h3><p className="mt-2 text-sm text-muted">Post edilen belge ve ödemeler burada görünecek.</p></div>
        )}
      </section>
    </div>
  );
}

function Info({ title, rows }: { title: string; rows: Array<[string, string | null | undefined]> }) {
  return <section className="rounded-xl border border-border bg-surface p-4"><h2 className="text-sm font-semibold">{title}</h2><dl className="mt-3 space-y-2">{rows.map(([label, value]) => <div key={label} className="grid grid-cols-[7rem_1fr] gap-2 text-sm"><dt className="text-muted">{label}</dt><dd className="break-words font-medium">{value || "—"}</dd></div>)}</dl></section>;
}
