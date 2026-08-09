import Link from "next/link";
import { ActivationButton } from "@/modules/settings/components/activation-button";
import { SettingsPagination } from "@/modules/settings/components/settings-pagination";
import { SettingsStatusBadge } from "@/modules/settings/components/status-badge";
import { formatSettingsDate, formatTry } from "@/modules/settings/presentation";
import type { SettingsListQuery, ShippingMethodPage } from "@/modules/settings/types";

// Burada kargo yöntemlerini ücret, sıralama ve checkout uygunluğuyla kompakt tabloda gösteriyorum.
export function ShippingMethodList({ page, query }: { page: ShippingMethodPage; query: SettingsListQuery }) {
  return (
    <section className="overflow-hidden rounded-xl border border-border bg-surface" aria-labelledby="shipping-method-list-title">
      <div className="flex items-center justify-between gap-3 border-b border-border bg-surface-subtle/60 px-4 py-3 sm:px-5">
        <div><h2 id="shipping-method-list-title" className="text-base font-semibold text-foreground">Tanımlı yöntemler</h2><p className="mt-0.5 text-xs text-muted">Aktif yöntemler checkout sırasında müşteriye sunulur.</p></div>
        <span className="text-xs font-semibold text-muted">{page.totalCount} kayıt</span>
      </div>
      {page.items.length ? (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead className="border-b border-border bg-surface-subtle/40 text-xs font-semibold uppercase tracking-wide text-muted"><tr><th className="px-5 py-3">Kargo yöntemi</th><th className="px-3 py-3 text-right">Ücret</th><th className="px-3 py-3 text-right">Sıra</th><th className="px-3 py-3">Durum</th><th className="px-3 py-3">Güncelleme</th><th className="px-5 py-3 text-right">İşlemler</th></tr></thead>
            <tbody className="divide-y divide-border">
              {page.items.map((method) => (
                <tr key={method.id} className="hover:bg-surface-subtle/35">
                  <td className="px-5 py-3"><Link href={`/settings/shipping-methods/${method.id}`} className="font-semibold text-foreground hover:text-primary-hover">{method.name}</Link><p className="mt-0.5 font-mono text-[11px] text-muted">{method.id}</p></td>
                  <td className="px-3 py-3 text-right font-semibold tabular-nums text-foreground">{formatTry(method.fixedFee)}</td>
                  <td className="px-3 py-3 text-right tabular-nums text-muted">{method.displayOrder}</td>
                  <td className="px-3 py-3"><SettingsStatusBadge active={method.isActive} /></td>
                  <td className="px-3 py-3 text-xs text-muted">{formatSettingsDate(method.updatedAt ?? method.createdAt)}</td>
                  <td className="px-5 py-3"><div className="flex items-start justify-end gap-2"><Link href={`/settings/shipping-methods/${method.id}`} className="inline-flex min-h-9 items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground hover:bg-surface-subtle">Düzenle</Link><ActivationButton kind="shipping" id={method.id} isActive={method.isActive} /></div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : <div className="px-5 py-12 text-center"><p className="font-semibold text-foreground">Henüz kargo yöntemi yok</p><p className="mt-1 text-sm text-muted">Checkout sırasında teslimat seçeneği sunmak için ilk yöntemi oluşturun.</p><Link href="/settings/shipping-methods/new" className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Kargo yöntemi ekle</Link></div>}
      <SettingsPagination basePath="/settings/shipping-methods" query={query} totalCount={page.totalCount} totalPages={page.totalPages} />
    </section>
  );
}
