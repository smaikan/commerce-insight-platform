"use client";

import Link from "next/link";
import { forwardRef, useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { savePurchaseInvoiceAction } from "../actions";
import type { AccountingFormState, CurrentAccountOption, PurchaseInvoiceFormDraft, PurchaseInvoiceLineDraft, PurchaseVariantOption } from "../types";

const initialInvoiceFormState: AccountingFormState<PurchaseInvoiceFormDraft> = { status: "idle" };

export function PurchaseInvoiceForm({ initialDraft, suppliers, variants, lookupTruncated }: { initialDraft: PurchaseInvoiceFormDraft; suppliers: CurrentAccountOption[]; variants: PurchaseVariantOption[]; lookupTruncated: boolean }) {
  const router = useRouter();
  const errorRef = useRef<HTMLDivElement>(null);
  const addLineRef = useRef<HTMLButtonElement>(null);
  const submitGuardRef = useRef(false);
  const sequenceRef = useRef(initialDraft.lines.length + 1);
  const [draft, setDraft] = useState(initialDraft);
  const [state, formAction, pending] = useActionState<AccountingFormState<PurchaseInvoiceFormDraft>, FormData>(savePurchaseInvoiceAction, initialInvoiceFormState);

  // Burada başarısız server action'ın güvenli taslağını dinamik satır editörüne geri yüklüyorum.
  useEffect(() => {
    if (state.status === "error") errorRef.current?.focus();
    // Burada yönlendirme ve yerinde veri yenilemeyi aynı sonuç için birlikte çalıştırmıyorum.
    if (state.redirectHref) router.replace(state.redirectHref);
    else if (state.refresh) router.refresh();
    submitGuardRef.current = false;
  }, [router, state]);

  function updateLine(key: string, patch: Partial<PurchaseInvoiceLineDraft>): void {
    setDraft((current) => ({ ...current, lines: current.lines.map((line) => line.key === key ? { ...line, ...patch } : line) }));
  }

  function addLine(): void {
    const nextNumber = Math.max(0, ...draft.lines.map((line) => Number(line.lineNumber) || 0)) + 1;
    setDraft((current) => ({ ...current, lines: [...current.lines, { key: `new-${sequenceRef.current++}`, lineNumber: String(nextNumber), productVariantId: "", purchaseQuantity: "1", unitOfMeasure: "Adet", unitsPerPurchaseUnit: "1", priceEntryMode: "1", vatRate: "20", enteredUnitPrice: "0", isInvoiceDiscountEligible: true, hasAllocations: false }] }));
  }

  function removeLine(key: string): void {
    setDraft((current) => ({ ...current, lines: current.lines.filter((line) => line.key !== key) }));
    queueMicrotask(() => addLineRef.current?.focus());
  }

  return (
    <form action={formAction} onSubmit={(event) => { if (submitGuardRef.current) event.preventDefault(); else submitGuardRef.current = true; }} className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_20rem]">
      <input type="hidden" name="linesJson" value={JSON.stringify(draft.lines)} />
      {state.status === "error" ? <ActionMessage ref={errorRef} state={state} /> : null}

      <div className="space-y-4">
        <section className="rounded-xl border border-border bg-surface p-4 sm:p-5">
          <div className="border-b border-border pb-4"><h2 className="text-base font-semibold">Belge başlığı</h2><p className="mt-1 text-sm text-muted">Tedarikçi, belge numarası ve mali tarih bilgileri.</p></div>
          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <label className="text-sm font-medium sm:col-span-2">Tedarikçi *
              <select id="currentAccountId" name="currentAccountId" required value={draft.currentAccountId} onChange={(event) => setDraft({ ...draft, currentAccountId: event.target.value })} aria-invalid={Boolean(fieldError(state, "currentAccountId", "Header.CurrentAccountId"))} className={inputClass}>
                <option value="">Tedarikçi seçin</option>{suppliers.map((supplier) => <option key={supplier.id} value={supplier.id}>{supplier.code} — {supplier.name}</option>)}
              </select><FieldError message={fieldError(state, "currentAccountId", "Header.CurrentAccountId")} />
              <span className="mt-1 block text-xs font-normal text-muted">Yalnız aktif Supplier veya CustomerAndSupplier cari hesaplar gösterilir. <Link href="/accounting/current-accounts/new" className="font-semibold text-primary hover:text-primary-hover">Yeni cari aç</Link></span>
            </label>
            <Field id="invoiceNumber" name="invoiceNumber" label="Fatura numarası" value={draft.invoiceNumber} required maxLength={100} error={fieldError(state, "invoiceNumber", "Header.InvoiceNumber")} onChange={(value) => setDraft({ ...draft, invoiceNumber: value })} />
            <Field id="invoiceDate" name="invoiceDate" label="Fatura tarihi" type="date" value={draft.invoiceDate} required error={fieldError(state, "invoiceDate", "Header.InvoiceDate")} onChange={(value) => setDraft({ ...draft, invoiceDate: value })} />
            <Field id="dueDate" name="dueDate" label="Vade tarihi" type="date" value={draft.dueDate} error={fieldError(state, "dueDate", "Header.DueDate")} onChange={(value) => setDraft({ ...draft, dueDate: value })} />
            <label className="text-sm font-medium sm:col-span-2">Açıklama
              <textarea id="description" name="description" rows={3} maxLength={500} value={draft.description} onChange={(event) => setDraft({ ...draft, description: event.target.value })} className={`${inputClass} py-2`} />
              <FieldError message={fieldError(state, "description", "Header.Description")} />
            </label>
          </div>
        </section>

        <section className="overflow-hidden rounded-xl border border-border bg-surface">
          <div className="flex flex-col gap-3 border-b border-border px-4 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-5"><div><h2 className="text-base font-semibold">Fatura satırları</h2><p className="mt-1 text-sm text-muted">Ürün kimliği katalogdan snapshot alınır; toplamlar kayıttan sonra API tarafından hesaplanır.</p></div><button ref={addLineRef} type="button" onClick={addLine} className="inline-flex min-h-10 cursor-pointer items-center justify-center rounded-lg border border-border-strong bg-surface px-3 text-sm font-semibold hover:bg-surface-subtle">Satır ekle</button></div>
          <div className="space-y-4 p-4 sm:p-5">
            {draft.lines.map((line, index) => <InvoiceLineEditor key={line.key} line={line} index={index} variants={variants} canRemove={draft.lines.length > 1 && !line.hasAllocations} onChange={(patch) => updateLine(line.key, patch)} onRemove={() => removeLine(line.key)} state={state} />)}
            {draft.lines.length === 0 ? <p className="rounded-lg border border-danger/30 bg-red-50 p-3 text-sm text-red-900">Faturada en az bir satır bulunmalıdır. “Satır ekle” ile devam edin.</p> : null}
          </div>
        </section>
      </div>

      <aside className="space-y-4 lg:sticky lg:top-20">
        <section className="rounded-xl border border-border bg-surface p-4"><h2 className="text-sm font-semibold">Belge ilkesi</h2><dl className="mt-3 space-y-3 text-sm"><div><dt className="text-xs font-semibold text-muted">Para birimi</dt><dd className="mt-0.5 font-semibold">TRY · Kur 1</dd></div><div><dt className="text-xs font-semibold text-muted">Stok etkisi</dt><dd className="mt-0.5 leading-5">Bu belge yeni fiziksel StockMovement oluşturmaz.</dd></div><div><dt className="text-xs font-semibold text-muted">Durum</dt><dd className="mt-0.5">Taslak olarak kaydedilir.</dd></div></dl></section>
        <section className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-950"><h2 className="font-semibold">İndirim sözleşmesi</h2><p className="mt-2 leading-6">API detay yanıtı indirim yapılandırmasını geri döndürmediği için bu güvenli form indirim oluşturmaz. API tarafından hesaplanan mevcut indirim toplamları detayda görünür.</p></section>
        {lookupTruncated ? <section className="rounded-xl border border-border bg-surface p-4"><h2 className="text-sm font-semibold">Seçim sınırı</h2><p className="mt-2 text-sm leading-6 text-muted">API özel lookup/search sağlamadığı için ilk 100 cari ve ürün kaydı tarandı. Aradığınız kayıt yoksa sözleşme geliştirmesi gerekir.</p></section> : null}
      </aside>

      <div className="flex flex-col-reverse gap-2 border-t border-border pt-4 sm:flex-row sm:justify-end lg:col-span-2">
        <Link href="/accounting/purchase-invoices" className="inline-flex min-h-11 items-center justify-center rounded-lg border border-border-strong bg-surface px-4 text-sm font-semibold hover:bg-surface-subtle">Vazgeç</Link>
        <button type="submit" disabled={pending || draft.lines.length === 0} aria-busy={pending} className="inline-flex min-h-11 cursor-pointer items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Kaydediliyor…" : "Taslak oluştur"}</button>
      </div>
    </form>
  );
}

function InvoiceLineEditor({ line, index, variants, canRemove, onChange, onRemove, state }: { line: PurchaseInvoiceLineDraft; index: number; variants: PurchaseVariantOption[]; canRemove: boolean; onChange: (patch: Partial<PurchaseInvoiceLineDraft>) => void; onRemove: () => void; state: AccountingFormState<PurchaseInvoiceFormDraft> }) {
  const existing = !line.key.startsWith("new-");
  const selectedKnown = variants.some((variant) => variant.id === line.productVariantId);
  return (
    <fieldset aria-label={`Fatura satırı ${line.lineNumber}`} className="rounded-lg border border-border bg-surface-subtle/35 p-3 sm:p-4">
      <legend className="px-1 text-sm font-semibold">Satır {line.lineNumber}</legend>
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <label className="text-xs font-semibold text-muted md:col-span-2">Ürün varyantı *
          <select id={`line-${index}-productVariantId`} value={line.productVariantId} disabled={existing} onChange={(event) => onChange({ productVariantId: event.target.value })} className={`${inputClass} text-sm disabled:cursor-not-allowed disabled:bg-surface-subtle`}>
            <option value="">Varyant seçin</option>{!selectedKnown && line.productVariantId ? <option value={line.productVariantId}>Mevcut varyant · {line.productVariantId.slice(0, 8)}</option> : null}{variants.map((variant) => <option key={variant.id} value={variant.id}>{variant.productName} · {variant.variantName} · {variant.sku}</option>)}
          </select><FieldError message={fieldError(state, `lines.${index}.productVariantId`, `Lines[${index}].ProductVariantId`)} />
          {existing ? <span className="mt-1 block font-normal">Katalog snapshot kimliği kayıttan sonra değiştirilemez.</span> : null}
        </label>
        <CompactField id={`line-${index}-purchaseQuantity`} label="Alış miktarı" type="number" step="0.0001" min="0.0001" value={line.purchaseQuantity} error={fieldError(state, `lines.${index}.purchaseQuantity`, `Lines[${index}].PurchaseQuantity`)} onChange={(value) => onChange({ purchaseQuantity: value })} />
        <CompactField id={`line-${index}-unitsPerPurchaseUnit`} label="Birim katsayısı" type="number" step="0.0001" min="0.0001" value={line.unitsPerPurchaseUnit} error={fieldError(state, `lines.${index}.unitsPerPurchaseUnit`, `Lines[${index}].UnitsPerPurchaseUnit`)} onChange={(value) => onChange({ unitsPerPurchaseUnit: value })} />
        <CompactField id={`line-${index}-unitOfMeasure`} label="Ölçü birimi" value={line.unitOfMeasure} maxLength={50} error={fieldError(state, `lines.${index}.unitOfMeasure`, `Lines[${index}].UnitOfMeasure`)} onChange={(value) => onChange({ unitOfMeasure: value })} />
        <label className="text-xs font-semibold text-muted">Fiyat giriş şekli
          <select id={`line-${index}-priceEntryMode`} value={line.priceEntryMode} onChange={(event) => onChange({ priceEntryMode: event.target.value })} className={`${inputClass} text-sm`}><option value="1">KDV hariç</option><option value="2">KDV dahil</option></select>
        </label>
        <CompactField id={`line-${index}-enteredUnitPrice`} label="Girilen birim fiyat" type="number" step="0.01" min="0" value={line.enteredUnitPrice} error={fieldError(state, `lines.${index}.enteredUnitPrice`, `Lines[${index}].EnteredUnitPrice`)} onChange={(value) => onChange({ enteredUnitPrice: value })} />
        <CompactField id={`line-${index}-vatRate`} label="KDV oranı (%)" type="number" step="0.01" min="0" max="100" value={line.vatRate} error={fieldError(state, `lines.${index}.vatRate`, `Lines[${index}].VatRate`)} onChange={(value) => onChange({ vatRate: value })} />
      </div>
      <div className="mt-3 flex justify-end border-t border-border pt-3">
        <button type="button" onClick={onRemove} disabled={!canRemove} aria-describedby={!canRemove ? `line-${index}-remove-help` : undefined} className="min-h-9 cursor-pointer rounded-lg border border-danger/30 bg-surface px-3 text-xs font-semibold text-danger hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-60">Satır {line.lineNumber}’i sil</button>
      </div>
      {!canRemove ? <p id={`line-${index}-remove-help`} className="mt-2 text-xs text-muted">{line.hasAllocations ? "Tahsisli satır toplu düzenlemede silinemez." : "Faturada en az bir satır kalmalıdır."}</p> : null}
    </fieldset>
  );
}

const inputClass = "mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft";

function Field({ id, name, label, value, onChange, type = "text", required, maxLength, error }: { id: string; name: string; label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean; maxLength?: number; error?: string }) {
  return <label className="text-sm font-medium">{label}{required ? " *" : ""}<input id={id} name={name} type={type} value={value} onChange={(event) => onChange(event.target.value)} required={required} maxLength={maxLength} aria-invalid={Boolean(error)} className={inputClass} /><FieldError message={error} /></label>;
}

function CompactField({ id, label, value, onChange, type = "text", step, min, max, maxLength, error }: { id: string; label: string; value: string; onChange: (value: string) => void; type?: string; step?: string; min?: string; max?: string; maxLength?: number; error?: string }) {
  return <label className="text-xs font-semibold text-muted">{label} *<input id={id} type={type} value={value} onChange={(event) => onChange(event.target.value)} step={step} min={min} max={max} maxLength={maxLength} aria-invalid={Boolean(error)} className={`${inputClass} text-sm`} /><FieldError message={error} /></label>;
}

function FieldError({ message }: { message?: string }) { return message ? <span className="mt-1 block text-xs font-semibold text-danger">{message}</span> : null; }

function fieldError(state: AccountingFormState<PurchaseInvoiceFormDraft>, ...keys: string[]): string | undefined {
  for (const key of keys) { const message = state.fieldErrors?.[key]?.[0]; if (message) return message; }
  return undefined;
}

const ActionMessage = forwardRef<HTMLDivElement, { state: AccountingFormState<PurchaseInvoiceFormDraft> }>(function ActionMessage({ state }, ref) {
  return <div ref={ref} role="alert" tabIndex={-1} className="rounded-xl border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900 outline-none focus:ring-2 focus:ring-danger/30 lg:col-span-2"><strong>{state.message}</strong>{state.traceId ? <span className="mt-1 block font-mono text-xs">Takip kodu: {state.traceId}</span> : null}</div>;
});
