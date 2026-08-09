"use client";

import Link from "next/link";
import { useActionState } from "react";
import { createCatalogItemAction, updateCatalogItemAction } from "@/modules/settings/catalog-actions";
import { catalogResourceConfigs, type CatalogResource } from "@/modules/settings/catalog-resource";
import type { CatalogItem } from "@/modules/settings/catalog-types";
import { getSettingsFieldError, SettingsActionError, SettingsField, settingsInputClass } from "@/modules/settings/components/form-controls";
import { initialSettingsActionState } from "@/modules/settings/types";

// Burada kaynak tipine göre yalnızca API'nin desteklediği katalog alanlarını gösteriyorum.
export function CatalogForm({ resource, item }: { resource: CatalogResource; item?: CatalogItem }) {
  const config = catalogResourceConfigs[resource];
  const action = item ? updateCatalogItemAction.bind(null, resource, item.id) : createCatalogItemAction.bind(null, resource);
  const [state, formAction, pending] = useActionState(action, initialSettingsActionState);
  const url = item && "url" in item ? item.url : "";
  const description = item && "description" in item ? item.description : "";
  return (
    <form action={formAction} className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_260px] lg:items-start">
      <section className="overflow-hidden rounded-xl border border-border bg-surface" aria-labelledby="catalog-form-title">
        <header className="border-b border-border bg-surface-subtle/60 px-4 py-3.5 sm:px-5"><h2 id="catalog-form-title" className="text-base font-semibold text-foreground">{config.singularTitle} bilgileri</h2><p className="mt-1 text-sm text-muted">Ürün yönetiminde kullanılacak temel tanımı düzenleyin.</p></header>
        <div className="grid gap-4 p-4 sm:p-5">
          <SettingsField label="Ad" htmlFor="catalog-name" error={getSettingsFieldError(state, "name")}><input id="catalog-name" name="name" required maxLength={150} defaultValue={item?.name} className={settingsInputClass} /></SettingsField>
          {config.supportsUrl ? <SettingsField label="URL değeri" htmlFor="catalog-url" error={getSettingsFieldError(state, "url")} hint="Boş bırakırsanız backend uygun değeri üretebilir."><input id="catalog-url" name="url" maxLength={200} defaultValue={url ?? ""} placeholder="ornek-deger" className={settingsInputClass} /></SettingsField> : null}
          {config.supportsDescription ? <SettingsField label="Açıklama" htmlFor="catalog-description" error={getSettingsFieldError(state, "description")}><textarea id="catalog-description" name="description" maxLength={1000} defaultValue={description ?? ""} rows={5} className={`${settingsInputClass} min-h-28 py-2.5`} /></SettingsField> : null}
        </div>
      </section>
      <aside className="space-y-4">
        {!item ? <section className="rounded-xl border border-border bg-surface p-4"><h2 className="text-sm font-semibold text-foreground">Kullanılabilirlik</h2><label className="mt-3 flex items-start gap-3 rounded-lg border border-border bg-surface-subtle/50 p-3"><input name="isActive" type="checkbox" defaultChecked className="mt-0.5 size-4 rounded border-border-strong text-primary focus:ring-focus" /><span><span className="block text-sm font-semibold text-foreground">Aktif oluştur</span><span className="mt-0.5 block text-xs leading-5 text-muted">Kayıt oluşturulduğunda ürün formlarında kullanılabilir.</span></span></label></section> : <section className="rounded-xl border border-border bg-surface p-4"><h2 className="text-sm font-semibold text-foreground">Mevcut durum</h2><p className="mt-2 text-sm text-muted">Bu kayıt şu anda <strong className="text-foreground">{item.isActive ? "aktif" : "pasif"}</strong>. Aktiflik liste ekranından değiştirilir.</p></section>}
        <section className="rounded-xl border border-primary/20 bg-primary/5 p-4 text-sm text-muted"><h2 className="font-semibold text-foreground">Katalog etkisi</h2><p className="mt-1 leading-5">Bu tanım ürünlerin sınıflandırılması ve yönetim formlarındaki seçenekler için kullanılır.</p></section>
      </aside>
      {state.status === "error" ? <SettingsActionError state={state} className="lg:col-span-2" /> : null}
      <div className="flex flex-col-reverse gap-2 border-t border-border pt-4 sm:flex-row sm:justify-end lg:col-span-2"><Link href={`/settings/catalog/${resource}`} className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle">Vazgeç</Link><button type="submit" disabled={pending} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Kaydediliyor…" : item ? "Değişiklikleri kaydet" : `${config.singularTitle} oluştur`}</button></div>
    </form>
  );
}
