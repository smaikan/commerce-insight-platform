"use client";

import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { cancelSalesOrderAction, postSalesInvoiceAction, postSalesOrderAction } from "../actions";
import type { SalesFormState } from "../types";

export function SalesOrderLifecycleActions({ orderId, orderNumber, invoiceId, status }: { orderId: string; orderNumber: string; invoiceId?: string | null; status: number }) {
  if (status === 1) return <PostAction action={postSalesOrderAction.bind(null, orderId)} title={`${orderNumber} post edilsin mi?`} />;
  if (status === 2) return <CancelOrderAction orderId={orderId} orderNumber={orderNumber} invoiceId={invoiceId ?? undefined} />;
  return <ReadOnlyNotice />;
}

export function SalesInvoiceLifecycleActions({ invoiceId, invoiceNumber, orderId, status }: { invoiceId: string; invoiceNumber: string; orderId: string; status: number }) {
  if (status === 1) return <PostAction action={postSalesInvoiceAction.bind(null, invoiceId, orderId)} title={`${invoiceNumber} ve bağlı satış post edilsin mi?`} />;
  if (status === 2) return <p className="rounded-lg border border-border bg-surface-subtle p-3 text-sm leading-6 text-muted">Fatura ayrı iptal edilemez. İptal, bağlı muhasebe satışı üzerinden stok, FIFO, alacak ve fatura için tek transaction olarak yürütülür.</p>;
  return <ReadOnlyNotice />;
}

function PostAction({ action, title }: { action: (_previous: SalesFormState) => Promise<SalesFormState>; title: string }) {
  const router = useRouter(); const guard = useRef(false); const [confirming, setConfirming] = useState(false);
  const [state, formAction, pending] = useActionState<SalesFormState, FormData>(action, { status: "idle" });
  useEffect(() => { if (state.refresh) router.refresh(); guard.current = false; }, [router, state]);
  return <div className="space-y-3">{!confirming ? <button type="button" onClick={() => setConfirming(true)} className="min-h-10 w-full cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Muhasebeleştir</button> : <form action={formAction} onSubmit={(event) => { if (guard.current) event.preventDefault(); else guard.current = true; }} aria-busy={pending} className="rounded-lg border border-primary/30 bg-primary-soft/40 p-3"><h3 className="text-sm font-semibold">{title}</h3><p className="mt-1 text-xs leading-5 text-muted">API stok çıkışı, FIFO tüketimi ve müşteri alacağını atomik oluşturur. İşlem stok koşulu değişmişse otomatik retry yapılmaz.</p><div className="mt-3 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><button type="button" disabled={pending} onClick={() => setConfirming(false)} className="min-h-10 cursor-pointer rounded-lg border border-border-strong bg-surface px-3 text-sm font-semibold disabled:cursor-not-allowed">Vazgeç</button><button type="submit" disabled={pending} className="min-h-10 cursor-pointer rounded-lg bg-primary px-3 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Muhasebeleştiriliyor…" : "Onayla ve muhasebeleştir"}</button></div></form>}{state.status !== "idle" ? <Message state={state} /> : null}</div>;
}

function CancelOrderAction({ orderId, orderNumber, invoiceId }: { orderId: string; orderNumber: string; invoiceId?: string }) {
  const router = useRouter(); const guard = useRef(false); const [open, setOpen] = useState(false);
  const [state, action, pending] = useActionState<SalesFormState, FormData>(cancelSalesOrderAction.bind(null, orderId, invoiceId), { status: "idle" });
  useEffect(() => { if (state.refresh) router.refresh(); guard.current = false; }, [router, state]);
  return <div className="space-y-3">{!open ? <button type="button" onClick={() => setOpen(true)} className="min-h-10 w-full cursor-pointer rounded-lg border border-danger/40 bg-surface px-4 text-sm font-semibold text-danger hover:bg-red-50">Muhasebe satışını iptal et</button> : <form action={action} onSubmit={(event) => { if (guard.current) event.preventDefault(); else guard.current = true; }} aria-busy={pending} className="rounded-lg border border-danger/30 bg-red-50 p-3"><h3 className="text-sm font-semibold text-red-950">{orderNumber} iptal edilsin mi?</h3><p className="mt-1 text-xs leading-5 text-red-900">Stok çıkışı terslenir, FIFO katmanları geri yüklenir, alacak ve varsa bağlı fatura iptal edilir. Geçerli tahsilat tahsisi varsa API işlemi reddeder.</p><label className="mt-3 block text-xs font-semibold text-red-950">İptal gerekçesi *<textarea name="reason" required maxLength={500} rows={3} className="mt-1.5 w-full rounded-lg border border-red-300 bg-surface px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-focus" /></label><div className="mt-3 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><button type="button" disabled={pending} onClick={() => setOpen(false)} className="min-h-10 cursor-pointer rounded-lg border border-border-strong bg-surface px-3 text-sm font-semibold disabled:cursor-not-allowed">Vazgeç</button><button type="submit" disabled={pending} className="min-h-10 cursor-pointer rounded-lg bg-danger px-3 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">{pending ? "İptal ediliyor…" : "İptali onayla"}</button></div></form>}{state.status !== "idle" ? <Message state={state} /> : null}</div>;
}

function ReadOnlyNotice() { return <p className="rounded-lg border border-border bg-surface-subtle p-3 text-sm leading-6 text-muted">İptal edilmiş belge salt okunurdur; tarihsel stok, FIFO ve finansal kayıt bağları korunur.</p>; }
function Message({ state }: { state: SalesFormState }) { return <p role={state.status === "error" ? "alert" : "status"} className={`rounded-lg border p-3 text-sm ${state.status === "error" ? "border-red-200 bg-red-50 text-red-950" : "border-emerald-200 bg-emerald-50 text-emerald-950"}`}>{state.message}{state.traceId ? <span className="mt-1 block font-mono text-xs">Takip: {state.traceId}</span> : null}</p>; }
