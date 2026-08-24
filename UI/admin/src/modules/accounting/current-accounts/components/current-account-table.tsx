import Link from "next/link";
import { currentAccountTypeClass, currentAccountTypeLabel } from "@/modules/accounting/current-accounts/presentation";
import type { CurrentAccountPage } from "@/modules/accounting/current-accounts/types";

export function CurrentAccountTable({ page }: { page: CurrentAccountPage }) {
  if (!page.items.length) {
    return <div className="px-5 py-14 text-center"><h2 className="text-base font-semibold">Henüz cari hesap bulunmuyor</h2><p className="mt-2 text-sm text-muted">İlk müşteri veya tedarikçi cari hesabını oluşturarak başlayın.</p></div>;
  }

  return (
    <div role="region" aria-label="Cari hesap tablosu; yatay kaydırılabilir" tabIndex={0} className="overflow-x-auto bg-surface outline-none focus:ring-2 focus:ring-inset focus:ring-focus/30">
      <table className="w-full min-w-[920px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr>
            <th scope="col" className="sticky left-0 z-10 w-52 bg-surface-subtle px-4 py-2.5 sm:w-64">Kod / unvan</th>
            <th scope="col" className="px-3 py-2.5">Tür</th>
            <th scope="col" className="px-3 py-2.5">Vergi bilgisi</th>
            <th scope="col" className="px-3 py-2.5">İletişim</th>
            <th scope="col" className="px-3 py-2.5">Konum</th>
            <th scope="col" className="px-3 py-2.5">Durum</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((account) => (
            <tr key={account.id} className="hover:bg-primary-soft/20">
              <td className="sticky left-0 w-52 max-w-52 bg-surface px-4 py-3 sm:w-64 sm:max-w-64">
                <Link title={account.name} href={`/accounting/current-accounts/${encodeURIComponent(account.id)}`} className="block max-w-44 overflow-hidden text-ellipsis whitespace-nowrap font-semibold text-foreground hover:text-primary focus-visible:ring-2 focus-visible:ring-focus sm:max-w-56">{account.name}</Link>
                <span className="mt-0.5 block font-mono text-xs text-muted">{account.code}</span>
              </td>
              <td className="px-3 py-3"><span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-bold ${currentAccountTypeClass(account.type)}`}>{currentAccountTypeLabel(account.type)}</span></td>
              <td className="px-3 py-3"><span className="block">{account.taxNumber || account.nationalIdentityNumber || "—"}</span><span className="text-xs text-muted">{account.taxOffice || "Vergi dairesi yok"}</span></td>
              <td className="px-3 py-3"><span className="block">{account.email || "—"}</span><span className="text-xs text-muted">{account.phoneNumber || "Telefon yok"}</span></td>
              <td className="px-3 py-3">{[account.city, account.district].filter(Boolean).join(" / ") || "—"}</td>
              <td className="px-3 py-3"><span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-bold ${account.isActive ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-border bg-surface-subtle text-muted"}`}>{account.isActive ? "Aktif" : "Pasif"}</span></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
