"use client";

import { useActionState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import { createInvoiceFromOrderAction } from "../actions";
import type { InvoiceFromOrderDraft, SalesFormState } from "../types";

export function InvoiceFromOrderForm({ orderId, orderNumber, status }: { orderId: string; orderNumber: string; status: number }) {
  const router = useRouter(); const guard = useRef(false); const today = new Date().toISOString().slice(0, 10);
  const [state, action, pending] = useActionState<SalesFormState<InvoiceFromOrderDraft>, FormData>(createInvoiceFromOrderAction.bind(null, orderId), { status: "idle" });
  useEffect(() => { if (state.redirectHref) { router.replace(state.redirectHref); router.refresh(); } if (state.refresh) router.refresh(); guard.current = false; }, [router, state]);
  return <section className="rounded-xl border border-border bg-surface p-4"><h2 className="text-sm font-semibold">Bu satışı faturalandır</h2><p className="mt-1 text-xs leading-5 text-muted">{status === 2 ? "Post edilmiş satıştan oluşturulan fatura doğrudan post edilmiş olur." : "Fatura taslak oluşur ve bağlı satışla birlikte post edilir."} İkinci stok veya alacak etkisi yaratılmaz.</p><form action={action} onSubmit={(event) => { if (guard.current) event.preventDefault(); else guard.current = true; }} className="mt-4 space-y-3" aria-busy={pending}><Field name="invoiceNumber" label="Fatura numarası" required defaultValue={`F-${orderNumber}`} /><Field name="invoiceDate" label="Fatura tarihi" type="date" required defaultValue={today} /><Field name="dueDate" label="Vade tarihi" type="date" /><label className="block text-xs font-semibold text-muted">Açıklama<textarea name="description" maxLength={500} rows={2} className={inputClass} /></label><button type="submit" disabled={pending} className="min-h-10 w-full cursor-pointer rounded-lg bg-primary px-3 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Fatura oluşturuluyor…" : "Satış faturası oluştur"}</button></form>{state.status === "error" ? <p role="alert" className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-950">{state.message}{state.traceId ? <span className="mt-1 block font-mono">Takip: {state.traceId}</span> : null}</p> : null}</section>;
}

const inputClass = "mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft";
function Field({ name, label, type = "text", required, defaultValue }: { name: string; label: string; type?: string; required?: boolean; defaultValue?: string }) { return <label className="block text-xs font-semibold text-muted">{label}{required ? " *" : ""}<input name={name} type={type} required={required} maxLength={type === "text" ? 100 : undefined} defaultValue={defaultValue} className={inputClass} /></label>; }
