"use client";

import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { cancelPurchaseInvoiceAction, postPurchaseInvoiceAction } from "../actions";
import type { AccountingFormState } from "../types";

// Burada post ve iptal kararlarını statüye göre ayırıp her mutation sonrasında authoritative detayı yeniden okutuyorum.
export function PurchaseInvoiceLifecycleActions({ invoiceId, invoiceNumber, status, incompleteLines, hasExpenses, postDataUnavailable }: { invoiceId: string; invoiceNumber: string; status: number; incompleteLines: number[]; hasExpenses: boolean; postDataUnavailable: boolean }) {
  if (status === 1) return <PostInvoiceAction invoiceId={invoiceId} invoiceNumber={invoiceNumber} incompleteLines={incompleteLines} hasExpenses={hasExpenses} postDataUnavailable={postDataUnavailable} />;
  if (status === 2) return <CancelInvoiceAction invoiceId={invoiceId} invoiceNumber={invoiceNumber} />;
  return <p className="rounded-lg border border-border bg-surface-subtle p-3 text-sm leading-6 text-muted">İptal edilmiş belge salt okunurdur. Ters kayıt ve lifecycle geçmişi korunur.</p>;
}

function PostInvoiceAction({ invoiceId, invoiceNumber, incompleteLines, hasExpenses, postDataUnavailable }: { invoiceId: string; invoiceNumber: string; incompleteLines: number[]; hasExpenses: boolean; postDataUnavailable: boolean }) {
  const router = useRouter();
  const guard = useRef(false);
  const [confirming, setConfirming] = useState(false);
  const [state, action, pending] = useActionState<AccountingFormState, FormData>(postPurchaseInvoiceAction.bind(null, invoiceId), { status: "idle" });
  const blocked = incompleteLines.length > 0 || hasExpenses || postDataUnavailable;
  useEffect(() => { if (state.refresh) router.refresh(); guard.current = false; }, [router, state]);
  return <div className="space-y-3">
    {incompleteLines.length ? <p id="post-block-reason" className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm leading-6 text-amber-950"><strong>Muhasebeleştirme bekliyor.</strong> Tam tahsisi eksik satırlar: {incompleteLines.join(", ")}.</p> : null}
    {hasExpenses ? <p id="post-expense-block" className="rounded-lg border border-danger/30 bg-red-50 p-3 text-sm leading-6 text-red-950"><strong>Giderli fatura post edilemez.</strong> Backend post sırasında dağıtılmış gider maliyetini sıfırlayabildiği için hatalı FIFO katmanı üretmemek adına aksiyon geçici olarak kapalıdır.</p> : null}
    {postDataUnavailable ? <p id="post-data-block" className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm leading-6 text-amber-950"><strong>Post ön kontrolü tamamlanamadı.</strong> Gider veya tahsis yardımcı verileri okunamadığı için sayfayı yenilemeden muhasebeleştirme yapılamaz.</p> : null}
    {!confirming ? <button type="button" disabled={blocked} aria-describedby={postDataUnavailable ? "post-data-block" : hasExpenses ? "post-expense-block" : incompleteLines.length ? "post-block-reason" : undefined} onClick={() => setConfirming(true)} className="min-h-10 w-full cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">Muhasebeleştir</button> : (
      <form action={action} onSubmit={(event) => { if (guard.current) event.preventDefault(); else guard.current = true; }} className="rounded-lg border border-primary/30 bg-primary-soft/40 p-3" aria-busy={pending}>
        <h3 className="text-sm font-semibold">{invoiceNumber} muhasebeleştirilsin mi?</h3><p className="mt-1 text-xs leading-5 text-muted">Yeni fiziksel StockMovement oluşmaz. API tedarikçi borcu ve mevcut Purchase hareketlerine bağlı FIFO maliyet katmanları oluşturur.</p>
        <div className="mt-3 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><button type="button" disabled={pending} onClick={() => setConfirming(false)} className="min-h-10 cursor-pointer rounded-lg border border-border-strong bg-surface px-3 text-sm font-semibold disabled:cursor-not-allowed">Vazgeç</button><button type="submit" disabled={pending} className="min-h-10 cursor-pointer rounded-lg bg-primary px-3 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Muhasebeleştiriliyor…" : "Onayla ve muhasebeleştir"}</button></div>
      </form>
    )}
    {state.status !== "idle" ? <LifecycleMessage state={state} /> : null}
  </div>;
}

function CancelInvoiceAction({ invoiceId, invoiceNumber }: { invoiceId: string; invoiceNumber: string }) {
  const router = useRouter();
  const guard = useRef(false);
  const [open, setOpen] = useState(false);
  const [state, action, pending] = useActionState<AccountingFormState, FormData>(cancelPurchaseInvoiceAction.bind(null, invoiceId), { status: "idle" });
  useEffect(() => { if (state.refresh) router.refresh(); guard.current = false; }, [router, state]);
  return <div className="space-y-3">
    {!open ? <button type="button" onClick={() => setOpen(true)} className="min-h-10 w-full cursor-pointer rounded-lg border border-danger/40 bg-surface px-4 text-sm font-semibold text-danger hover:bg-red-50">Faturayı iptal et</button> : (
      <form action={action} onSubmit={(event) => { if (guard.current) event.preventDefault(); else guard.current = true; }} className="rounded-lg border border-danger/30 bg-red-50 p-3" aria-busy={pending}>
        <h3 className="text-sm font-semibold text-red-950">{invoiceNumber} iptal edilsin mi?</h3><p className="mt-1 text-xs leading-5 text-red-900">Tüketilmemiş katmanlar geçersizleşir ve supplier debt için ters kayıt oluşur. Tüketilmiş katman veya ödeme tahsisi varsa API işlemi reddeder; otomatik retry yapılmaz.</p>
        <label className="mt-3 block text-xs font-semibold text-red-950">İptal gerekçesi *<textarea name="reason" required maxLength={500} rows={3} className="mt-1.5 w-full rounded-lg border border-red-300 bg-surface px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-focus" /></label>
        <div className="mt-3 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><button type="button" disabled={pending} onClick={() => setOpen(false)} className="min-h-10 cursor-pointer rounded-lg border border-border-strong bg-surface px-3 text-sm font-semibold disabled:cursor-not-allowed">Vazgeç</button><button type="submit" disabled={pending} className="min-h-10 cursor-pointer rounded-lg bg-danger px-3 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">{pending ? "İptal ediliyor…" : "İptali onayla"}</button></div>
      </form>
    )}
    {state.status !== "idle" ? <LifecycleMessage state={state} /> : null}
  </div>;
}

function LifecycleMessage({ state }: { state: AccountingFormState }) {
  return <p role={state.status === "error" ? "alert" : "status"} className={`rounded-lg border p-3 text-sm ${state.status === "error" ? "border-red-200 bg-red-50 text-red-950" : "border-emerald-200 bg-emerald-50 text-emerald-950"}`}>{state.message}{state.traceId ? <span className="mt-1 block font-mono text-xs">Takip: {state.traceId}</span> : null}</p>;
}
