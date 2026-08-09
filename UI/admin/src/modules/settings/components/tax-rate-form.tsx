"use client";

import Link from "next/link";
import { useActionState } from "react";
import { createTaxRateAction, updateTaxRateAction } from "@/modules/settings/actions";
import { getSettingsFieldError, SettingsActionError, SettingsField, settingsInputClass } from "@/modules/settings/components/form-controls";
import { initialSettingsActionState, type TaxRate } from "@/modules/settings/types";

// Burada vergi oranı oluşturma ve düzenleme akışlarını aynı belgeli alanlarla sunuyorum.
export function TaxRateForm({ taxRate }: { taxRate?: TaxRate }) {
  const action = taxRate ? updateTaxRateAction.bind(null, taxRate.id) : createTaxRateAction;
  const [state, formAction, pending] = useActionState(action, initialSettingsActionState);
  const fieldError = (name: string) => getSettingsFieldError(state, name);

  return (
    <form action={formAction} className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_260px] lg:items-start">
      <section className="overflow-hidden rounded-xl border border-border bg-surface" aria-labelledby="tax-form-title">
        <header className="border-b border-border bg-surface-subtle/60 px-4 py-3.5 sm:px-5">
          <h2 id="tax-form-title" className="text-base font-semibold text-foreground">Vergi bilgileri</h2>
          <p className="mt-1 text-sm text-muted">Ürün yönetiminde tanınacak adı ve yüzde oranını belirleyin.</p>
        </header>
        <div className="grid gap-4 p-4 sm:grid-cols-2 sm:p-5">
          <SettingsField label="Vergi adı" htmlFor="tax-name" error={fieldError("name")}>
            <input id="tax-name" name="name" required maxLength={100} defaultValue={taxRate?.name} placeholder="Örn. Standart KDV" className={settingsInputClass} />
          </SettingsField>
          <SettingsField label="Vergi oranı" htmlFor="tax-rate" error={fieldError("rate")} hint="0 ile 100 arasında bir yüzde girin.">
            <div className="relative"><input id="tax-rate" name="rate" type="number" inputMode="decimal" min="0" max="100" step="0.01" required defaultValue={taxRate?.rate ?? 20} className={`${settingsInputClass} pr-10`} /><span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm font-semibold text-muted">%</span></div>
          </SettingsField>
        </div>
      </section>

      <aside className="space-y-4">
        {!taxRate ? (
          <section className="rounded-xl border border-border bg-surface p-4">
            <h2 className="text-sm font-semibold text-foreground">Kullanılabilirlik</h2>
            <label className="mt-3 flex items-start gap-3 rounded-lg border border-border bg-surface-subtle/50 p-3">
              <input name="isActive" type="checkbox" defaultChecked className="mt-0.5 size-4 rounded border-border-strong text-primary focus:ring-focus" />
              <span><span className="block text-sm font-semibold text-foreground">Aktif oluştur</span><span className="mt-0.5 block text-xs leading-5 text-muted">Oran oluşturulduğu anda ürünlerde seçilebilir.</span></span>
            </label>
          </section>
        ) : (
          <section className="rounded-xl border border-border bg-surface p-4">
            <h2 className="text-sm font-semibold text-foreground">Mevcut durum</h2>
            <p className="mt-2 text-sm text-muted">Bu oran şu anda <strong className="text-foreground">{taxRate.isActive ? "aktif" : "pasif"}</strong>. Aktiflik liste ekranındaki bağımsız işlemden değiştirilir.</p>
          </section>
        )}
        <section className="rounded-xl border border-primary/20 bg-primary/5 p-4 text-sm text-muted">
          <h2 className="font-semibold text-foreground">Hesaplama otoritesi</h2>
          <p className="mt-1 leading-5">Net fiyat ve sipariş vergisi backend tarafından hesaplanır. Panel yalnızca oran tanımını yönetir.</p>
        </section>
      </aside>

      {state.status === "error" ? <SettingsActionError state={state} className="lg:col-span-2" /> : null}
      <div className="flex flex-col-reverse gap-2 border-t border-border pt-4 sm:flex-row sm:justify-end lg:col-span-2">
        <Link href="/settings/tax-rates" className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle">Vazgeç</Link>
        <button type="submit" disabled={pending} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Kaydediliyor…" : taxRate ? "Değişiklikleri kaydet" : "Vergi oranı oluştur"}</button>
      </div>
    </form>
  );
}
