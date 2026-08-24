"use client";

import { useActionState, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { savePurchaseInvoiceAllocationsAction } from "../actions";
import type { AccountingFormState, AvailableStockMovement, PurchaseInvoice, PurchaseInvoiceAllocationDraft } from "../types";

type InvoiceLine = PurchaseInvoice["lines"][number];

// Burada tek fatura satırının fiziksel Purchase hareketi tahsislerini ayrı ve geri okunabilir bir intent olarak yönetiyorum.
export function PurchaseAllocationEditor({ invoiceId, line, movements }: { invoiceId: string; line: InvoiceLine; movements: AvailableStockMovement[] }) {
  const router = useRouter();
  const guardRef = useRef(false);
  const existingByMovement = useMemo(() => new Map(line.allocations.map((item) => [item.stockMovementId, item.allocatedQuantity])), [line.allocations]);
  const unavailableExisting = line.allocations.filter((allocation) => !movements.some((movement) => movement.id === allocation.stockMovementId));
  const [quantities, setQuantities] = useState<Record<string, string>>(() => Object.fromEntries(movements.map((movement) => [movement.id, existingByMovement.get(movement.id) ? String(existingByMovement.get(movement.id)) : ""])));
  const action = savePurchaseInvoiceAllocationsAction.bind(null, invoiceId, line.id);
  const [state, formAction, pending] = useActionState<AccountingFormState<PurchaseInvoiceAllocationDraft>, FormData>(action, { status: "idle" });
  const allocations = movements.flatMap((movement) => { const quantity = Number(quantities[movement.id]); return Number.isInteger(quantity) && quantity > 0 ? [{ stockMovementId: movement.id, quantity: String(quantity) }] : []; });
  const allocated = line.allocations.reduce((sum, item) => sum + item.allocatedQuantity, 0);

  useEffect(() => {
    if (state.refresh) router.refresh();
    guardRef.current = false;
  }, [router, state]);

  return (
    <details className="group border-t border-border bg-surface-subtle/25">
      <summary className="flex min-h-11 cursor-pointer list-none items-center justify-between gap-3 px-4 py-2 text-sm font-semibold marker:hidden hover:bg-surface-subtle">
        <span>Tahsis yönetimi</span><span className={allocated === line.stockQuantity ? "text-success" : "text-warning"}>{allocated} / {line.stockQuantity} stok birimi · {allocated === line.stockQuantity ? "Tamam" : `${Math.max(0, line.stockQuantity - allocated)} eksik`}</span>
      </summary>
      <div className="border-t border-border px-4 py-4">
        {unavailableExisting.length ? (
          <div role="alert" className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-950"><strong>Mevcut tahsis güvenle düzenlenemiyor.</strong><p className="mt-1 leading-5">API tamamen dolu hareketi seçim listesinden çıkarırken mevcut tahsis DTO’su hareket kapasitesini döndürmüyor. Tahsis korunuyor; sözleşme düzelene kadar bu satırda overwrite yapılmayacak.</p></div>
        ) : movements.length ? (
          <form action={formAction} onSubmit={(event) => { if (guardRef.current) event.preventDefault(); else guardRef.current = true; }} aria-busy={pending} className="space-y-3">
            <input type="hidden" name="allocationsJson" value={JSON.stringify(allocations)} />
            <div className="overflow-x-auto"><table className="w-full min-w-[640px] text-left text-xs"><thead className="text-muted"><tr><th className="pb-2">Purchase hareketi</th><th className="pb-2">Tarih</th><th className="pb-2 text-right">Fiziksel</th><th className="pb-2 text-right">Başka tahsisler</th><th className="pb-2 text-right">Bu satıra tahsis</th></tr></thead><tbody className="divide-y divide-border">{movements.map((movement) => { const current = existingByMovement.get(movement.id) ?? 0; const capacity = movement.availableQuantity + current; return <tr key={movement.id}><td className="py-2 font-mono" title={movement.id}>{movement.id.slice(0, 12)}…</td><td className="py-2">{new Intl.DateTimeFormat("tr-TR", { dateStyle: "short", timeZone: "UTC" }).format(new Date(movement.createdAt))}</td><td className="py-2 text-right tabular-nums">{movement.quantity}</td><td className="py-2 text-right tabular-nums">{Math.max(0, movement.allocatedQuantity - current)}</td><td className="py-2 text-right"><label className="sr-only" htmlFor={`allocation-${line.id}-${movement.id}`}>{movement.id} hareketinden Satır {line.lineNumber} tahsis miktarı</label><input id={`allocation-${line.id}-${movement.id}`} type="number" inputMode="numeric" min="0" max={capacity} step="1" value={quantities[movement.id] ?? ""} onChange={(event) => setQuantities({ ...quantities, [movement.id]: event.target.value })} className="min-h-9 w-24 rounded-lg border border-border-strong bg-surface px-2 text-right tabular-nums outline-none focus:ring-2 focus:ring-focus" /></td></tr>; })}</tbody></table></div>
            {state.status !== "idle" ? <p role={state.status === "error" ? "alert" : "status"} className={`rounded-lg border px-3 py-2 text-sm ${state.status === "error" ? "border-red-200 bg-red-50 text-red-900" : "border-emerald-200 bg-emerald-50 text-emerald-900"}`}>{state.message}{state.traceId ? <span className="mt-1 block font-mono text-xs">Takip: {state.traceId}</span> : null}</p> : null}
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between"><p className="text-xs leading-5 text-muted">Kaydetme bütün mevcut tahsisleri bu değerlerle değiştirir. Toplam, satırın {line.stockQuantity} stok birimini aşamaz.</p><button type="submit" disabled={pending || allocations.length === 0} className="min-h-10 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Tahsis ediliyor…" : "Tahsisleri kaydet"}</button></div>
          </form>
        ) : <p className="rounded-lg border border-border bg-surface p-3 text-sm text-muted">Bu varyant için tahsise açık pozitif Purchase StockMovement bulunmuyor.</p>}
      </div>
    </details>
  );
}
