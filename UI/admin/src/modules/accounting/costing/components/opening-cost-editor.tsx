"use client";

import { useActionState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { updateOpeningCostAction } from "../actions";
import { initialOpeningCostActionState, type OpeningBalanceCostLayer } from "../types";
import { formatAccountingMoney } from "@/modules/accounting/core/presentation";

// Burada güncel katman ile korunmuş kullanıcı taslağını aynı panelde karşılaştırarak açık yeniden onay istiyorum.
export function OpeningCostEditor({ layer }: { layer: OpeningBalanceCostLayer }) {
  const router = useRouter();
  const [state, action, pending] = useActionState(updateOpeningCostAction, initialOpeningCostActionState);
  const current = state.currentLayer || layer;
  const draft = state.draft;
  useEffect(() => { if (state.refresh) router.refresh(); }, [router, state.refresh]);

  return (
    <section className="rounded-xl border border-border bg-surface">
      <div className="border-b border-border px-5 py-4">
        <p className="text-xs font-bold uppercase tracking-[0.12em] text-primary">Açılış katmanı</p>
        <h2 className="mt-1 text-lg font-semibold">Birim maliyet düzeltmesi</h2>
        <p className="mt-1 text-sm leading-6 text-muted">Yalnız açılış stoğunun maliyeti değişir; fiziksel miktar ve FIFO denetim izi korunur.</p>
      </div>

      {state.status === "conflict" ? (
        <div role="alert" className="m-5 rounded-lg border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950">
          <p className="font-semibold">Başka bir değişiklik algılandı</p>
          <p className="mt-1 leading-6">{state.message}</p>
          <dl className="mt-3 grid gap-3 sm:grid-cols-2">
            <div><dt className="text-xs font-bold uppercase tracking-wide">Sizin taslağınız · KDV hariç</dt><dd className="mt-1 font-semibold tabular-nums">{draft?.unitCostExcludingVat || "—"}</dd></div>
            <div><dt className="text-xs font-bold uppercase tracking-wide">API’deki güncel değer · KDV hariç</dt><dd className="mt-1 font-semibold tabular-nums">{formatAccountingMoney(current.unitCostExcludingVat)}</dd></div>
            <div><dt className="text-xs font-bold uppercase tracking-wide">Sizin taslağınız · KDV dahil</dt><dd className="mt-1 font-semibold tabular-nums">{draft?.unitCostIncludingVat || "Otomatik"}</dd></div>
            <div><dt className="text-xs font-bold uppercase tracking-wide">API’deki güncel değer · KDV dahil</dt><dd className="mt-1 font-semibold tabular-nums">{formatAccountingMoney(current.unitCostIncludingVat)}</dd></div>
          </dl>
        </div>
      ) : null}

      <form action={action} className="grid gap-4 p-5 sm:grid-cols-2">
        <input type="hidden" name="layerId" value={current.id} />
        <input type="hidden" name="productVariantId" value={current.productVariantId} />
        <input type="hidden" name="expectedConcurrencyToken" value={current.concurrencyToken} />
        <label className="text-sm font-medium">KDV hariç birim maliyet *<input name="unitCostExcludingVat" inputMode="decimal" required pattern="\d{1,16}([.,]\d{1,2})?" defaultValue={draft?.unitCostExcludingVat ?? current.unitCostExcludingVat.toFixed(2)} aria-describedby="unit-cost-excluding-error" className="mt-1.5 min-h-11 w-full rounded-lg border border-border-strong bg-white px-3 tabular-nums" />{state.fieldErrors?.unitCostExcludingVat ? <span id="unit-cost-excluding-error" className="mt-1 block text-xs text-danger">{state.fieldErrors.unitCostExcludingVat.join(" ")}</span> : null}</label>
        <label className="text-sm font-medium">KDV dahil birim maliyet<input name="unitCostIncludingVat" inputMode="decimal" pattern="\d{1,16}([.,]\d{1,2})?" defaultValue={draft?.unitCostIncludingVat ?? current.unitCostIncludingVat.toFixed(2)} aria-describedby="unit-cost-including-error" className="mt-1.5 min-h-11 w-full rounded-lg border border-border-strong bg-white px-3 tabular-nums" />{state.fieldErrors?.unitCostIncludingVat ? <span id="unit-cost-including-error" className="mt-1 block text-xs text-danger">{state.fieldErrors.unitCostIncludingVat.join(" ")}</span> : null}</label>
        <div className="sm:col-span-2">
          <dl className="grid gap-3 rounded-lg bg-surface-subtle p-4 text-sm sm:grid-cols-3">
            <div><dt className="text-muted">Açılış miktarı</dt><dd className="mt-1 font-semibold tabular-nums">{current.originalQuantity}</dd></div>
            <div><dt className="text-muted">Kalan FIFO miktarı</dt><dd className="mt-1 font-semibold tabular-nums">{current.remainingQuantity}</dd></div>
            <div><dt className="text-muted">Katman durumu</dt><dd className="mt-1 font-semibold">{current.status === 1 ? "Aktif" : current.status === 2 ? "Tükendi" : "Geçersiz"}</dd></div>
          </dl>
        </div>
        {state.status === "error" ? <p role="alert" className="sm:col-span-2 text-sm text-danger">{state.message}{state.traceId ? ` İz: ${state.traceId}` : ""}</p> : null}
        {state.status === "success" ? <p role="status" className="sm:col-span-2 text-sm text-success">{state.message}</p> : null}
        <div className="flex justify-end sm:col-span-2">
          <button type="submit" disabled={pending} className="min-h-11 cursor-pointer rounded-lg bg-primary px-5 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Kaydediliyor…" : state.status === "conflict" ? "Güncel kaydı kullanıp maliyeti uygula" : "Maliyeti güncelle"}</button>
        </div>
      </form>
    </section>
  );
}
