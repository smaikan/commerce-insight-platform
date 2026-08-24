import Link from "next/link";
import { ActivationButton } from "@/modules/settings/components/activation-button";
import { SettingsPagination } from "@/modules/settings/components/settings-pagination";
import { SettingsStatusBadge } from "@/modules/settings/components/status-badge";
import { formatRate, formatSettingsDate } from "@/modules/settings/presentation";
import type { SettingsListQuery, TaxRatePage } from "@/modules/settings/types";

// Burada vergi oranlarını yüzde, durum ve güncelleme bilgileriyle kompakt tabloda gösteriyorum.
export function TaxRateList({ page, query }: { page: TaxRatePage; query: SettingsListQuery }) {
  return (
    <section className="overflow-hidden rounded-xl border border-border bg-surface" aria-labelledby="tax-rate-list-title">
      <div className="flex items-center justify-between gap-3 border-b border-border bg-surface-subtle/60 px-4 py-3 sm:px-5">
        <div><h2 id="tax-rate-list-title" className="text-base font-semibold text-foreground">Tanımlı oranlar</h2><p className="mt-0.5 text-xs text-muted">Aktif oranlar ürün ve vergi hesaplama seçimlerinde kullanılabilir.</p></div>
        <span className="text-xs font-semibold text-muted">{page.totalCount} kayıt</span>
      </div>
      {page.items.length ? (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[680px] text-left text-sm">
            <thead className="border-b border-border bg-surface-subtle/40 text-xs font-semibold uppercase tracking-wide text-muted"><tr><th className="px-5 py-3">Vergi adı</th><th className="px-3 py-3 text-right">Oran</th><th className="px-3 py-3">Durum</th><th className="px-3 py-3">Güncelleme</th><th className="px-5 py-3 text-right">İşlemler</th></tr></thead>
            <tbody className="divide-y divide-border">
              {page.items.map((rate) => (
                <tr key={rate.id} className="hover:bg-surface-subtle/35">
                  <td className="px-5 py-3"><Link href={`/settings/tax-rates/${rate.id}`} className="font-semibold text-foreground hover:text-primary-hover">{rate.name}</Link><p className="mt-0.5 font-mono text-[11px] text-muted">{rate.id}</p></td>
                  <td className="px-3 py-3 text-right text-base font-semibold tabular-nums text-foreground">{formatRate(rate.rate)}</td>
                  <td className="px-3 py-3"><SettingsStatusBadge active={rate.isActive} /></td>
                  <td className="px-3 py-3 text-xs text-muted">{formatSettingsDate(rate.updatedAt ?? rate.createdAt)}</td>
                  <td className="px-5 py-3"><div className="flex items-start justify-end gap-2"><Link href={`/settings/tax-rates/${rate.id}`} className="inline-flex min-h-9 cursor-pointer items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground transition-colors hover:bg-surface-subtle">Düzenle</Link><ActivationButton kind="tax" id={rate.id} isActive={rate.isActive} /></div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : <div className="px-5 py-12 text-center"><p className="font-semibold text-foreground">Henüz vergi oranı yok</p><p className="mt-1 text-sm text-muted">Ürünlerde kullanılacak ilk vergi oranını oluşturun.</p><Link href="/settings/tax-rates/new" className="mt-4 inline-flex min-h-10 cursor-pointer items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white transition-colors hover:bg-primary-hover">Vergi oranı ekle</Link></div>}
      <SettingsPagination basePath="/settings/tax-rates" query={query} totalCount={page.totalCount} totalPages={page.totalPages} />
    </section>
  );
}
