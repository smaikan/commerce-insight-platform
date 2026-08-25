"use client";
/* eslint-disable react/no-unescaped-entities */

import Link from "next/link";
import { forwardRef, useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { createDirectSalesInvoiceAction, saveSalesOrderAction } from "../actions";
import { newSalesLine } from "../presentation";
import type { CurrentAccountOption, SalesFormState, SalesLineDraft, SalesOrderFormDraft, SalesVariantOption } from "../types";

const initialState: SalesFormState<SalesOrderFormDraft> = { status: "idle" };

export function SalesDocumentForm({ mode, orderId, initialDraft, customers, variants, lookupTruncated }: { mode: "sales-order" | "direct-invoice"; orderId?: string; initialDraft: SalesOrderFormDraft; customers: CurrentAccountOption[]; variants: SalesVariantOption[]; lookupTruncated: boolean }) {
  const router = useRouter();
  const errorRef = useRef<HTMLDivElement>(null);
  const submitGuard = useRef(false);
  const sequence = useRef(initialDraft.lines.length + 1);
  const [draft, setDraft] = useState(initialDraft);
  const action = mode === "direct-invoice" ? createDirectSalesInvoiceAction : saveSalesOrderAction.bind(null, orderId);
  const [state, formAction, pending] = useActionState<SalesFormState<SalesOrderFormDraft>, FormData>(action, initialState);
  const isEdit = Boolean(orderId);

  useEffect(() => {
    if (state.status === "error") errorRef.current?.focus();
    // Burada route değişimi gereken başarıyla yalnız mevcut veriyi yenileyen sonucu birbirinden ayırıyorum.
    if (state.redirectHref) router.replace(state.redirectHref);
    else if (state.refresh) router.refresh();
    submitGuard.current = false;
  }, [router, state]);

  function updateLine(key: string, patch: Partial<SalesLineDraft>): void {
    setDraft((current) => ({ ...current, lines: current.lines.map((line) => line.key === key ? { ...line, ...patch } : line) }));
  }
  function addLine(): void {
    const number = Math.max(0, ...draft.lines.map((line) => Number(line.lineNumber) || 0)) + 1;
    setDraft((current) => ({ ...current, lines: [...current.lines, newSalesLine(number, `new-${sequence.current++}`)] }));
  }
  function removeLine(key: string): void { setDraft((current) => ({ ...current, lines: current.lines.filter((line) => line.key !== key) })); }

  const showInvoice = mode === "direct-invoice" || (!isEdit && draft.createInvoice);
  const cancelHref = mode === "direct-invoice" ? "/accounting/sales-invoices" : isEdit ? `/accounting/sales-orders/${orderId}` : "/accounting/sales-orders";
  return (
    <form action={formAction} onSubmit={(event) => { if (submitGuard.current) event.preventDefault(); else submitGuard.current = true; }} className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_20rem]">
      <input type="hidden" name="idempotencyKey" value={draft.idempotencyKey} />
      <input type="hidden" name="linesJson" value={JSON.stringify(draft.lines)} />
      {mode === "direct-invoice" ? <input type="hidden" name="createInvoice" value="on" /> : null}
      {state.status === "error" ? <ActionMessage ref={errorRef} state={state} /> : null}
      <div className="space-y-4">
        <Section title="Muhasebe satışı" description="E-ticaret siparişi veya sepeti değil; doğrudan müşteri cari hesabına bağlı ön muhasebe belgesi.">
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="text-sm font-medium sm:col-span-2">Müşteri cari hesabı *
              <select name="currentAccountId" required value={draft.currentAccountId} onChange={(event) => setDraft({ ...draft, currentAccountId: event.target.value })} className={inputClass}><option value="">Müşteri seçin</option>{customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.code} — {customer.name}</option>)}</select>
              <FieldError message={fieldError(state, "currentAccountId", "Header.CurrentAccountId", "OrderHeader.CurrentAccountId")} />
            </label>
            <Field name="orderNumber" label="Satış numarası" required maxLength={100} value={draft.orderNumber} error={fieldError(state, "orderNumber", "Header.OrderNumber", "OrderHeader.OrderNumber")} onChange={(value) => setDraft({ ...draft, orderNumber: value })} />
            <Field name="orderDate" label="Satış tarihi" type="date" required value={draft.orderDate} error={fieldError(state, "orderDate")} onChange={(value) => setDraft({ ...draft, orderDate: value })} />
            <Field name="dueDate" label="Vade tarihi" type="date" value={draft.dueDate} error={fieldError(state, "dueDate")} onChange={(value) => setDraft({ ...draft, dueDate: value })} />
            <Field name="shippingTotal" label="Kargo tutarı" type="number" step="0.01" min="0" required value={draft.shippingTotal} error={fieldError(state, "shippingTotal")} onChange={(value) => setDraft({ ...draft, shippingTotal: value })} />
            <label className="text-sm font-medium">Kargo ödeyeni *<select name="shippingPayer" value={draft.shippingPayer} onChange={(event) => setDraft({ ...draft, shippingPayer: event.target.value })} className={inputClass}><option value="0">Yok</option><option value="1">Satıcı</option><option value="2">Müşteri</option></select><FieldError message={fieldError(state, "shippingPayer")} /></label>
            <label className="text-sm font-medium sm:col-span-2">Açıklama<textarea name="description" rows={3} maxLength={500} value={draft.description} onChange={(event) => setDraft({ ...draft, description: event.target.value })} className={`${inputClass} py-2`} /><FieldError message={fieldError(state, "description")} /></label>
          </div>
        </Section>
        <Section title="Fatura düzeyi indirim" description="İndirim kullanılmıyorsa üç alanı da boş bırakın; tutarlar API tarafından hesaplanır.">
          <div className="grid gap-4 sm:grid-cols-3"><label className="text-sm font-medium">Tür<select name="invoiceDiscountType" value={draft.invoiceDiscountType} onChange={(event) => setDraft({ ...draft, invoiceDiscountType: event.target.value, invoiceDiscountValue: event.target.value ? draft.invoiceDiscountValue : "", invoiceDiscountTaxBasis: event.target.value ? draft.invoiceDiscountTaxBasis : "" })} className={inputClass}><option value="">İndirim yok</option><option value="1">Yüzde</option><option value="4">Sabit fatura toplamı</option></select><FieldError message={fieldError(state, "invoiceDiscountType")} /></label><Field name="invoiceDiscountValue" label="Değer" type="number" step="0.01" min="0" value={draft.invoiceDiscountValue} error={fieldError(state, "invoiceDiscountValue")} onChange={(value) => setDraft({ ...draft, invoiceDiscountValue: value })} /><label className="text-sm font-medium">Vergi bazı<select name="invoiceDiscountTaxBasis" value={draft.invoiceDiscountTaxBasis} onChange={(event) => setDraft({ ...draft, invoiceDiscountTaxBasis: event.target.value })} className={inputClass}><option value="">Seçin</option><option value="1">KDV hariç</option><option value="2">KDV dahil</option></select><FieldError message={fieldError(state, "invoiceDiscountTaxBasis")} /></label></div>
        </Section>
        <section className="overflow-hidden rounded-xl border border-border bg-surface"><div className="flex flex-col gap-3 border-b border-border p-4 sm:flex-row sm:items-center sm:justify-between sm:px-5"><div><h2 className="text-base font-semibold">Satış satırları</h2><p className="mt-1 text-sm text-muted">Stok ve FIFO etkisi yalnız post işleminde oluşur.</p></div><button type="button" onClick={addLine} className="min-h-10 cursor-pointer rounded-lg border border-border-strong px-3 text-sm font-semibold hover:bg-surface-subtle">Satır ekle</button></div><div className="space-y-4 p-4 sm:p-5">{draft.lines.map((line, index) => <SalesLineEditor key={line.key} line={line} index={index} variants={variants} canRemove={draft.lines.length > 1} onChange={(patch) => updateLine(line.key, patch)} onRemove={() => removeLine(line.key)} state={state} />)}{draft.lines.length === 0 ? <p role="alert" className="rounded-lg border border-danger/30 bg-red-50 p-3 text-sm text-red-900">En az bir satış satırı ekleyin.</p> : null}</div></section>
        {showInvoice ? <InvoiceHeaderFields draft={draft} setDraft={setDraft} state={state} /> : null}
      </div>
      <aside className="space-y-4 lg:sticky lg:top-20">
        {!isEdit && mode === "sales-order" ? <section className="rounded-xl border border-border bg-surface p-4"><label className="flex cursor-pointer gap-3"><input name="createInvoice" type="checkbox" checked={draft.createInvoice} onChange={(event) => setDraft({ ...draft, createInvoice: event.target.checked })} className="mt-0.5 size-4 cursor-pointer" /><span><strong className="block text-sm">Satışla birlikte iç fatura oluştur</strong><span className="mt-1 block text-xs leading-5 text-muted">Fatura aynı satışa bağlanır; ikinci stok veya müşteri alacağı oluşturmaz.</span></span></label></section> : null}
        <section className="rounded-xl border border-border bg-surface p-4"><h2 className="text-sm font-semibold">Belge ilkesi</h2><dl className="mt-3 space-y-3 text-sm"><div><dt className="text-xs font-semibold text-muted">Alan</dt><dd className="mt-0.5">AccountingSalesOrder</dd></div><div><dt className="text-xs font-semibold text-muted">Para birimi</dt><dd className="mt-0.5 font-semibold">TRY · Kur 1</dd></div><div><dt className="text-xs font-semibold text-muted">İlk durum</dt><dd className="mt-0.5">Taslak · stok etkisiz</dd></div></dl></section>
        <section className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-950"><h2 className="font-semibold">Eşzamanlı düzenleme</h2><p className="mt-2 leading-6">API düzenleme isteğinde concurrency token taşımıyor. 409 durumunda belge yeniden okunur; değişiklikler otomatik uygulanmaz.</p></section>
        {lookupTruncated ? <section className="rounded-xl border border-border bg-surface p-4 text-sm"><h2 className="font-semibold">Seçim sınırı</h2><p className="mt-2 leading-6 text-muted">Özel lookup/search endpoint'i olmadığı için ilk 100 cari ve ürün tarandı.</p></section> : null}
      </aside>
      <div className="flex flex-col-reverse gap-2 border-t border-border pt-4 sm:flex-row sm:justify-end lg:col-span-2"><Link href={cancelHref} className="inline-flex min-h-11 items-center justify-center rounded-lg border border-border-strong bg-surface px-4 text-sm font-semibold hover:bg-surface-subtle">Vazgeç</Link><button type="submit" disabled={pending || draft.lines.length === 0} aria-busy={pending} className="inline-flex min-h-11 cursor-pointer items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Kaydediliyor…" : isEdit ? "Taslağı güncelle" : mode === "direct-invoice" ? "Satış faturası oluştur" : "Muhasebe satışı oluştur"}</button></div>
    </form>
  );
}

export function SalesLineEditor({ line, index, variants, canRemove, onChange, onRemove, state }: { line: SalesLineDraft; index: number; variants: SalesVariantOption[]; canRemove: boolean; onChange: (patch: Partial<SalesLineDraft>) => void; onRemove: () => void; state: SalesFormState<unknown> }) {
  const existing = !line.key.startsWith("new-");
  const selectedKnown = variants.some((variant) => variant.id === line.productVariantId);
  const prefix = `lines.${index}`;
  return <fieldset className="rounded-lg border border-border bg-surface-subtle/35 p-3 sm:p-4"><legend className="px-1 text-sm font-semibold">Satış satırı {line.lineNumber}</legend><div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
    <label className="text-xs font-semibold text-muted md:col-span-2">Ürün varyantı *<select value={line.productVariantId} disabled={existing} onChange={(event) => onChange({ productVariantId: event.target.value })} className={`${inputClass} text-sm disabled:cursor-not-allowed disabled:bg-surface-subtle`}><option value="">Varyant seçin</option>{!selectedKnown && line.productVariantId ? <option value={line.productVariantId}>Mevcut varyant · {line.productVariantId.slice(0, 8)}</option> : null}{variants.map((variant) => <option key={variant.id} value={variant.id}>{variant.productName} · {variant.variantName} · {variant.sku}</option>)}</select><FieldError message={fieldError(state, `${prefix}.productVariantId`)} /></label>
    <Compact label="Satış miktarı" type="number" step="0.0001" min="0.0001" value={line.quantity} error={fieldError(state, `${prefix}.quantity`)} onChange={(value) => onChange({ quantity: value })} />
    <Compact label="Birim katsayısı" type="number" step="0.0001" min="0.0001" value={line.unitsPerSaleUnit} error={fieldError(state, `${prefix}.unitsPerSaleUnit`)} onChange={(value) => onChange({ unitsPerSaleUnit: value })} />
    <Compact label="Ölçü birimi" value={line.unitOfMeasure} error={fieldError(state, `${prefix}.unitOfMeasure`)} onChange={(value) => onChange({ unitOfMeasure: value })} />
    <label className="text-xs font-semibold text-muted">Fiyat giriş şekli<select value={line.priceEntryMode} onChange={(event) => onChange({ priceEntryMode: event.target.value })} className={`${inputClass} text-sm`}><option value="1">KDV hariç</option><option value="2">KDV dahil</option></select></label>
    <Compact label="Birim fiyat" type="number" step="0.01" min="0" value={line.enteredUnitPrice} error={fieldError(state, `${prefix}.enteredUnitPrice`)} onChange={(value) => onChange({ enteredUnitPrice: value })} />
    <Compact label="KDV (%)" type="number" step="0.01" min="0" max="100" value={line.vatRate} error={fieldError(state, `${prefix}.vatRate`)} onChange={(value) => onChange({ vatRate: value })} />
  </div><div className="mt-3 grid gap-3 border-t border-border pt-3 md:grid-cols-2 xl:grid-cols-4"><label className="text-xs font-semibold text-muted">Satır indirimi<select value={line.lineDiscountType} onChange={(event) => onChange({ lineDiscountType: event.target.value, lineDiscountValue: event.target.value ? line.lineDiscountValue : "", lineDiscountTaxBasis: event.target.value ? line.lineDiscountTaxBasis : "", lineDiscountUnitBasis: event.target.value === "2" ? line.lineDiscountUnitBasis : "" })} className={`${inputClass} text-sm`}><option value="">İndirim yok</option><option value="1">Yüzde</option><option value="2">Sabit birim</option><option value="3">Sabit satır</option></select></label><Compact label="İndirim değeri" type="number" step="0.01" min="0" value={line.lineDiscountValue} error={fieldError(state, `${prefix}.lineDiscountValue`)} onChange={(value) => onChange({ lineDiscountValue: value })} /><label className="text-xs font-semibold text-muted">Vergi bazı<select value={line.lineDiscountTaxBasis} onChange={(event) => onChange({ lineDiscountTaxBasis: event.target.value })} className={`${inputClass} text-sm`}><option value="">Seçin</option><option value="1">KDV hariç</option><option value="2">KDV dahil</option></select></label>{line.lineDiscountType === "2" ? <label className="text-xs font-semibold text-muted">Birim bazı<select value={line.lineDiscountUnitBasis} onChange={(event) => onChange({ lineDiscountUnitBasis: event.target.value })} className={`${inputClass} text-sm`}><option value="">Seçin</option><option value="1">Alış birimi</option><option value="2">Satış birimi</option><option value="3">Stok birimi</option></select></label> : <div />}</div><label className="mt-3 flex cursor-pointer items-center gap-2 text-xs font-medium text-muted"><input type="checkbox" checked={line.isInvoiceDiscountEligible} onChange={(event) => onChange({ isInvoiceDiscountEligible: event.target.checked })} className="size-4 cursor-pointer" />Fatura indirimine uygun</label><div className="mt-3 flex justify-end"><button type="button" onClick={onRemove} disabled={!canRemove} className="min-h-9 cursor-pointer rounded-lg border border-danger/30 px-3 text-xs font-semibold text-danger hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-60">Satır {line.lineNumber}’i sil</button></div></fieldset>;
}

function InvoiceHeaderFields({ draft, setDraft, state }: { draft: SalesOrderFormDraft; setDraft: React.Dispatch<React.SetStateAction<SalesOrderFormDraft>>; state: SalesFormState<SalesOrderFormDraft> }) {
  return <Section title="İç satış faturası" description="Aynı muhasebe satışının belge katmanıdır; stok/FIFO/alacak etkisi tekrar oluşturulmaz."><div className="grid gap-4 sm:grid-cols-2"><Field name="invoiceNumber" label="Fatura numarası" required maxLength={100} value={draft.invoiceNumber} error={fieldError(state, "invoiceInvoiceNumber", "invoiceNumber")} onChange={(value) => setDraft({ ...draft, invoiceNumber: value })} /><Field name="invoiceDate" label="Fatura tarihi" type="date" required value={draft.invoiceDate} error={fieldError(state, "invoiceInvoiceDate", "invoiceDate")} onChange={(value) => setDraft({ ...draft, invoiceDate: value })} /><Field name="invoiceDueDate" label="Fatura vade tarihi" type="date" value={draft.invoiceDueDate} error={fieldError(state, "invoiceDueDate")} onChange={(value) => setDraft({ ...draft, invoiceDueDate: value })} /><label className="text-sm font-medium sm:col-span-2">Fatura açıklaması<textarea name="invoiceDescription" rows={3} maxLength={500} value={draft.invoiceDescription} onChange={(event) => setDraft({ ...draft, invoiceDescription: event.target.value })} className={`${inputClass} py-2`} /></label></div></Section>;
}

const inputClass = "mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft";
function Section({ title, description, children }: { title: string; description: string; children: React.ReactNode }) { return <section className="rounded-xl border border-border bg-surface p-4 sm:p-5"><div className="border-b border-border pb-4"><h2 className="text-base font-semibold">{title}</h2><p className="mt-1 text-sm text-muted">{description}</p></div><div className="mt-5">{children}</div></section>; }
function Field({ name, label, value, onChange, type = "text", required, maxLength, min, step, error }: { name: string; label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean; maxLength?: number; min?: string; step?: string; error?: string }) { return <label className="text-sm font-medium">{label}{required ? " *" : ""}<input name={name} type={type} value={value} onChange={(event) => onChange(event.target.value)} required={required} maxLength={maxLength} min={min} step={step} aria-invalid={Boolean(error)} className={inputClass} /><FieldError message={error} /></label>; }
function Compact({ label, value, onChange, type = "text", step, min, max, error }: { label: string; value: string; onChange: (value: string) => void; type?: string; step?: string; min?: string; max?: string; error?: string }) { return <label className="text-xs font-semibold text-muted">{label} *<input type={type} value={value} onChange={(event) => onChange(event.target.value)} step={step} min={min} max={max} aria-invalid={Boolean(error)} className={`${inputClass} text-sm`} /><FieldError message={error} /></label>; }
function FieldError({ message }: { message?: string }) { return message ? <span className="mt-1 block text-xs font-semibold text-danger">{message}</span> : null; }
function fieldError(state: SalesFormState<unknown>, ...keys: string[]): string | undefined { for (const key of keys) { const value = state.fieldErrors?.[key]?.[0]; if (value) return value; } return undefined; }
const ActionMessage = forwardRef<HTMLDivElement, { state: SalesFormState<SalesOrderFormDraft> }>(function ActionMessage({ state }, ref) { return <div ref={ref} role="alert" tabIndex={-1} className="rounded-xl border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900 outline-none focus:ring-2 focus:ring-danger/30 lg:col-span-2"><strong>{state.message}</strong>{state.traceId ? <span className="mt-1 block font-mono text-xs">Takip kodu: {state.traceId}</span> : null}</div>; });
