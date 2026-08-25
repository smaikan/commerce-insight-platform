"use client";
/* eslint-disable react/no-unescaped-entities */

import Link from "next/link";
import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { updateSalesInvoiceAction } from "../actions";
import { newSalesLine } from "../presentation";
import type { SalesFormState, SalesInvoice, SalesInvoiceEditDraft, SalesLineDraft, SalesVariantOption } from "../types";
import { SalesLineEditor } from "./sales-document-form";

export function SalesInvoiceEditForm({ invoice, initialDraft, variants, lookupTruncated }: { invoice: SalesInvoice; initialDraft: SalesInvoiceEditDraft; variants: SalesVariantOption[]; lookupTruncated: boolean }) {
  const router = useRouter();
  const errorRef = useRef<HTMLDivElement>(null);
  const guard = useRef(false);
  const sequence = useRef(initialDraft.lines.length + 1);
  const [draft, setDraft] = useState(initialDraft);
  const [state, action, pending] = useActionState<SalesFormState<SalesInvoiceEditDraft>, FormData>(updateSalesInvoiceAction.bind(null, invoice.id), { status: "idle" });
  // Burada satış faturası sonucunda route geçişi ile çakışma yenilemesini aynı anda başlatmıyorum.
  useEffect(() => { if (state.status === "error") errorRef.current?.focus(); if (state.redirectHref) router.replace(state.redirectHref); else if (state.refresh) router.refresh(); guard.current = false; }, [router, state]);
  function updateLine(key: string, patch: Partial<SalesLineDraft>): void { setDraft((current) => ({ ...current, lines: current.lines.map((line) => line.key === key ? { ...line, ...patch } : line) })); }
  function addLine(): void { const number = Math.max(0, ...draft.lines.map((line) => Number(line.lineNumber) || 0)) + 1; setDraft((current) => ({ ...current, lines: [...current.lines, newSalesLine(number, `new-${sequence.current++}`)] })); }
  return <form action={action} onSubmit={(event) => { if (guard.current) event.preventDefault(); else guard.current = true; }} className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_20rem]">
    <input type="hidden" name="linesJson" value={JSON.stringify(draft.lines)} />
    {state.status === "error" ? <div ref={errorRef} role="alert" tabIndex={-1} className="rounded-xl border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900 outline-none focus:ring-2 focus:ring-danger/30 lg:col-span-2"><strong>{state.message}</strong>{state.traceId ? <span className="mt-1 block font-mono text-xs">Takip kodu: {state.traceId}</span> : null}</div> : null}
    <div className="space-y-4">
      <section className="rounded-xl border border-border bg-surface p-4 sm:p-5"><div className="border-b border-border pb-4"><h2 className="text-base font-semibold">Fatura başlığı</h2><p className="mt-1 text-sm text-muted">Yalnız iç fatura numarası ve tarih alanları; müşteri ve satış başlığı bağlı satışın sahibidir.</p></div><div className="mt-5 grid gap-4 sm:grid-cols-2"><Field name="invoiceNumber" label="Fatura numarası" required value={draft.invoiceNumber} onChange={(value) => setDraft({ ...draft, invoiceNumber: value })} /><Field name="invoiceDate" label="Fatura tarihi" type="date" required value={draft.invoiceDate} onChange={(value) => setDraft({ ...draft, invoiceDate: value })} /><Field name="dueDate" label="Vade tarihi" type="date" value={draft.dueDate} onChange={(value) => setDraft({ ...draft, dueDate: value })} /><label className="text-sm font-medium sm:col-span-2">Açıklama<textarea name="description" maxLength={500} rows={3} value={draft.description} onChange={(event) => setDraft({ ...draft, description: event.target.value })} className={`${inputClass} py-2`} /></label></div></section>
      <section className="overflow-hidden rounded-xl border border-border bg-surface"><div className="flex flex-col gap-3 border-b border-border p-4 sm:flex-row sm:items-center sm:justify-between sm:px-5"><div><h2 className="text-base font-semibold">Fatura ve bağlı satış satırları</h2><p className="mt-1 text-sm text-muted">Bu tam satır listesi bağlı Draft AccountingSalesOrder'ı aynı transaction içinde günceller.</p></div><button type="button" onClick={addLine} className="min-h-10 cursor-pointer rounded-lg border border-border-strong px-3 text-sm font-semibold hover:bg-surface-subtle">Satır ekle</button></div><div className="space-y-4 p-4 sm:p-5">{draft.lines.map((line, index) => <SalesLineEditor key={line.key} line={line} index={index} variants={variants} canRemove={draft.lines.length > 1} onChange={(patch) => updateLine(line.key, patch)} onRemove={() => setDraft((current) => ({ ...current, lines: current.lines.filter((item) => item.key !== line.key) }))} state={state} />)}</div></section>
    </div>
    <aside className="space-y-4 lg:sticky lg:top-20"><section className="rounded-xl border border-border bg-surface p-4"><h2 className="text-sm font-semibold">Bağlı muhasebe satışı</h2><p className="mt-2 text-sm leading-6 text-muted">Müşteri: <strong className="text-foreground">{invoice.currentAccountName}</strong></p><Link href={`/accounting/sales-orders/${encodeURIComponent(invoice.accountingSalesOrderId)}`} className="mt-3 inline-flex min-h-9 items-center text-sm font-semibold text-primary hover:text-primary-hover">Bağlı satışa git →</Link></section><section className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-950"><h2 className="font-semibold">Eşzamanlı düzenleme</h2><p className="mt-2 leading-6">API concurrency token sağlamıyor. 409 durumunda otomatik overwrite yapılmaz.</p></section>{lookupTruncated ? <section className="rounded-xl border border-border bg-surface p-4 text-sm text-muted">Ürün seçimi ilk 100 katalog kaydıyla sınırlıdır.</section> : null}</aside>
    <div className="flex flex-col-reverse gap-2 border-t border-border pt-4 sm:flex-row sm:justify-end lg:col-span-2"><Link href={`/accounting/sales-invoices/${invoice.id}`} className="inline-flex min-h-11 items-center justify-center rounded-lg border border-border-strong px-4 text-sm font-semibold hover:bg-surface-subtle">Vazgeç</Link><button type="submit" disabled={pending || draft.lines.length === 0} aria-busy={pending} className="min-h-11 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Kaydediliyor…" : "Fatura taslağını güncelle"}</button></div>
  </form>;
}

const inputClass = "mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface px-3 outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft";
function Field({ name, label, value, onChange, type = "text", required }: { name: string; label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean }) { return <label className="text-sm font-medium">{label}{required ? " *" : ""}<input name={name} type={type} required={required} maxLength={type === "text" ? 100 : undefined} value={value} onChange={(event) => onChange(event.target.value)} className={inputClass} /></label>; }
