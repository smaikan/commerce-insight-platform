import { formatAccountingDate, formatAccountingMoney } from "@/modules/accounting/core/presentation";
import type { ProductVariantCostHistory } from "../types";

// Burada varyant maliyet geçmişini geçerlilik aralığı ve stok snapshotlarıyla denetlenebilir bir zaman çizelgesinde gösteriyorum.
export function CostHistory({ history }: { history: ProductVariantCostHistory[] }) {
  return (
    <section className="overflow-hidden rounded-xl border border-border bg-surface">
      <div className="border-b border-border px-5 py-4"><p className="text-xs font-bold uppercase tracking-[0.12em] text-primary">Denetim izi</p><h2 className="mt-1 text-lg font-semibold">Varyant maliyet geçmişi</h2><p className="mt-1 text-sm text-muted">Satın alma ve açılış maliyeti değişimlerinin kapanmayan kayıt zinciri.</p></div>
      {!history.length ? <div className="px-5 py-12 text-center"><p className="font-semibold">Maliyet geçmişi bulunmuyor</p><p className="mt-1 text-sm text-muted">Bu varyant için henüz bir maliyet kaydı üretilmemiş.</p></div> : (
        <div role="region" aria-label="Varyant maliyet geçmişi; yatay kaydırılabilir" tabIndex={0} className="overflow-x-auto outline-none focus:ring-2 focus:ring-inset focus:ring-focus/30">
          <table className="w-full min-w-[900px] border-collapse text-left text-sm">
            <thead className="border-b border-border bg-surface-subtle text-[11px] font-bold uppercase tracking-[0.08em] text-muted"><tr><th scope="col" className="px-4 py-2.5">Geçerlilik</th><th scope="col" className="px-3 py-2.5">Kaynak</th><th scope="col" className="px-3 py-2.5 text-right">Önceki</th><th scope="col" className="px-3 py-2.5 text-right">Yeni · KDV hariç</th><th scope="col" className="px-3 py-2.5 text-right">Yeni · KDV dahil</th><th scope="col" className="px-4 py-2.5 text-right">Stok açılış / kapanış</th></tr></thead>
            <tbody className="divide-y divide-border/80">{history.map((item) => <tr key={item.id} className="hover:bg-primary-soft/20"><td className="px-4 py-3"><span className="font-medium">{formatAccountingDate(item.validFrom)}</span><span className="mt-0.5 block text-xs text-muted">{item.validTo ? `${formatAccountingDate(item.validTo)} tarihine kadar` : "Güncel kayıt"}</span></td><td className="px-3 py-3">{item.sourceType === 1 ? "Alış faturası" : "Açılış düzeltmesi"}</td><td className="px-3 py-3 text-right tabular-nums">{item.previousCostExcludingVat == null ? "—" : formatAccountingMoney(item.previousCostExcludingVat)}</td><td className="px-3 py-3 text-right font-semibold tabular-nums">{formatAccountingMoney(item.newCostExcludingVat)}</td><td className="px-3 py-3 text-right tabular-nums">{formatAccountingMoney(item.newCostIncludingVat)}</td><td className="px-4 py-3 text-right tabular-nums">{item.openingStockQuantity} / {item.closingStockQuantity ?? "—"}</td></tr>)}</tbody>
          </table>
        </div>
      )}
    </section>
  );
}
