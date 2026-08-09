"use client";

import Link from "next/link";
import { useActionState } from "react";
import { createShippingMethodAction, updateShippingMethodAction } from "@/modules/settings/actions";
import { getSettingsFieldError, SettingsActionError, SettingsField, settingsInputClass } from "@/modules/settings/components/form-controls";
import { initialSettingsActionState, type ShippingMethod } from "@/modules/settings/types";

// Burada kargo yöntemi oluşturma ve düzenleme akışlarını aynı belgeli alanlarla sunuyorum.
export function ShippingMethodForm({ method }: { method?: ShippingMethod }) {
  const action = method ? updateShippingMethodAction.bind(null, method.id) : createShippingMethodAction;
  const [state, formAction, pending] = useActionState(action, initialSettingsActionState);
  const fieldError = (name: string) => getSettingsFieldError(state, name);

  return (
    <form action={formAction} className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_260px] lg:items-start">
      <section className="overflow-hidden rounded-xl border border-border bg-surface" aria-labelledby="shipping-form-title">
        <header className="border-b border-border bg-surface-subtle/60 px-4 py-3.5 sm:px-5">
          <h2 id="shipping-form-title" className="text-base font-semibold text-foreground">Yöntem bilgileri</h2>
          <p className="mt-1 text-sm text-muted">Checkout sırasında gösterilecek adı, sabit ücreti ve sıralamayı belirleyin.</p>
        </header>
        <div className="grid gap-4 p-4 sm:grid-cols-2 sm:p-5">
          <SettingsField label="Kargo yöntemi adı" htmlFor="shipping-name" error={fieldError("name")} className="sm:col-span-2">
            <input id="shipping-name" name="name" required maxLength={150} defaultValue={method?.name} placeholder="Örn. Standart Teslimat" className={settingsInputClass} />
          </SettingsField>
          <SettingsField label="Sabit ücret" htmlFor="shipping-fee" error={fieldError("fixedFee")} hint="Ücretsiz teslimat için 0 girin.">
            <div className="relative"><input id="shipping-fee" name="fixedFee" type="number" inputMode="decimal" min="0" step="0.01" required defaultValue={method?.fixedFee ?? 0} className={`${settingsInputClass} pr-12`} /><span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm font-semibold text-muted">TL</span></div>
          </SettingsField>
          <SettingsField label="Görüntülenme sırası" htmlFor="shipping-order" error={fieldError("displayOrder")} hint="Küçük değerler önce gösterilir.">
            <input id="shipping-order" name="displayOrder" type="number" inputMode="numeric" min="0" step="1" required defaultValue={method?.displayOrder ?? 0} className={settingsInputClass} />
          </SettingsField>
        </div>
      </section>

      <aside className="space-y-4">
        {!method ? (
          <section className="rounded-xl border border-border bg-surface p-4">
            <h2 className="text-sm font-semibold text-foreground">Kullanılabilirlik</h2>
            <label className="mt-3 flex items-start gap-3 rounded-lg border border-border bg-surface-subtle/50 p-3">
              <input name="isActive" type="checkbox" defaultChecked className="mt-0.5 size-4 rounded border-border-strong text-primary focus:ring-focus" />
              <span><span className="block text-sm font-semibold text-foreground">Aktif oluştur</span><span className="mt-0.5 block text-xs leading-5 text-muted">Yöntem oluşturulduğu anda checkout sırasında seçilebilir.</span></span>
            </label>
          </section>
        ) : (
          <section className="rounded-xl border border-border bg-surface p-4">
            <h2 className="text-sm font-semibold text-foreground">Mevcut durum</h2>
            <p className="mt-2 text-sm text-muted">Bu yöntem şu anda <strong className="text-foreground">{method.isActive ? "aktif" : "pasif"}</strong>. Aktiflik liste ekranındaki bağımsız işlemden değiştirilir.</p>
          </section>
        )}
        <section className="rounded-xl border border-primary/20 bg-primary/5 p-4 text-sm text-muted">
          <h2 className="font-semibold text-foreground">Checkout etkisi</h2>
          <p className="mt-1 leading-5">Kargo adı ve ücreti sipariş oluşturulurken backend tarafından yeniden doğrulanır ve siparişe snapshot olarak kaydedilir.</p>
        </section>
      </aside>

      {state.status === "error" ? <SettingsActionError state={state} className="lg:col-span-2" /> : null}
      <div className="flex flex-col-reverse gap-2 border-t border-border pt-4 sm:flex-row sm:justify-end lg:col-span-2">
        <Link href="/settings/shipping-methods" className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle">Vazgeç</Link>
        <button type="submit" disabled={pending} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Kaydediliyor…" : method ? "Değişiklikleri kaydet" : "Kargo yöntemi oluştur"}</button>
      </div>
    </form>
  );
}
