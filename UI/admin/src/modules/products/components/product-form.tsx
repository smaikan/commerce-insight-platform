"use client";

import Link from "next/link";
import { useActionState, useEffect, useState } from "react";
import { useFormStatus } from "react-dom";
import { createProductAction, updateProductAction } from "@/modules/products/actions";
import { productStatusOptions } from "@/modules/products/query";
import { initialProductActionState, type Product, type ProductFormOptions, type ProductImage } from "@/modules/products/types";
import { VariantEditor } from "@/modules/products/components/variant-editor";
import { ProductMediaEditor } from "@/modules/products/components/product-media-editor";
import { TagEditor } from "@/modules/products/components/tag-editor";

const inputClass = "mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground";

// Burada temel ürün endpoint'ini tetikleyen form alanlarını varyant ve durum kontrollerinden ayırıyorum.
const baseProductFieldNames = new Set([
  "title",
  "mainSku",
  "type",
  "url",
  "brandId",
  "description",
  "displayOrder",
  "seoTitle",
  "seoDescription",
  "taxRateId",
]);

// Burada ürün formunun oluşturma ve düzenleme akışını aynı karar gruplarında, server action durumuyla yönetiyorum.
export function ProductForm({
  mode,
  product,
  images = [],
  options,
}: {
  mode: "create" | "edit";
  product?: Product;
  images?: ProductImage[];
  options: ProductFormOptions;
}) {
  const action = mode === "create" ? createProductAction : updateProductAction;
  const [state, formAction] = useActionState(action, initialProductActionState);
  const [dirty, setDirty] = useState(false);
  const [baseChanged, setBaseChanged] = useState(mode === "create");

  // Burada kaydedilmemiş form değişikliklerinde sekme yenileme/kapatma riskini kullanıcıya bildiriyorum.
  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => {
      if (!dirty) return;
      event.preventDefault();
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  const fieldError = (name: string) => state.fieldErrors?.[name];

  // Burada yalnız temel ürün DTO'suna ait bir kontrol değiştiğinde temel güncelleme çağrısını etkinleştiriyorum.
  const handleFormChange = (event: React.FormEvent<HTMLFormElement>) => {
    setDirty(true);
    const control = event.target;
    if (
      (control instanceof HTMLInputElement || control instanceof HTMLTextAreaElement || control instanceof HTMLSelectElement)
      && baseProductFieldNames.has(control.name)
    ) {
      setBaseChanged(true);
    }
  };

  return (
    <form action={formAction} onChange={handleFormChange} className="pb-8">
      {baseChanged ? <input type="hidden" name="baseChanged" value="on" /> : null}
      {product ? (
        <>
          <input type="hidden" name="productId" value={product.id} />
          <input type="hidden" name="originalStatus" value={product.status} />
          <input type="hidden" name="originalIsFeatured" value={String(product.isFeatured)} />
          <input type="hidden" name="originalHasVariants" value={String(product.hasVariants)} />
        </>
      ) : null}

      {state.status !== "idle" ? (
        <div className={`mb-5 rounded-xl border px-4 py-3 text-sm ${state.status === "partial" ? "border-amber-300 bg-amber-50 text-amber-900" : "border-red-300 bg-red-50 text-red-900"}`} role="alert">
          <p className="font-semibold">{state.status === "partial" ? "Kısmi kayıt" : "Kayıt tamamlanamadı"}</p>
          <p className="mt-1 leading-6">{state.message}</p>
          {state.traceId ? <p className="mt-1 text-xs">Takip kodu: {state.traceId}</p> : null}
          {state.productId ? (
            <Link href={state.reloadHref || `/products/${state.productId}`} className="mt-2 inline-flex font-semibold underline">
              {state.reloadHref ? "Güncel kaydı yükle" : "Kaydedilen ürünü aç"}
            </Link>
          ) : null}
        </div>
      ) : null}

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <div className="space-y-6">
          <FormSection title="Temel bilgiler" description="Ürünün katalog kimliğini ve açıklamasını tanımlayın.">
            <div className="grid gap-4">
              <Field label="Ürün başlığı" name="title" defaultValue={product?.title || ""} maxLength={250} required error={fieldError("title")} />
              <div>
                <Field label="URL" name="url" defaultValue={product?.url || ""} maxLength={250} help="Boş bırakılırsa backend ürün başlığından oluşturabilir." error={fieldError("url")} />
              </div>
              <label className="block text-sm font-medium text-foreground">
                Açıklama
                <textarea name="description" defaultValue={product?.description || ""} maxLength={4000} rows={7} className={`${inputClass} resize-y py-3`} aria-invalid={Boolean(fieldError("description"))} />
                <FieldError messages={fieldError("description")} />
              </label>
            </div>
          </FormSection>

          <ProductMediaEditor images={images} />

          <VariantEditor
            variants={product?.variants || []}
            mode={mode}
            initialHasVariants={product?.hasVariants ?? false}
            initialMainSku={product?.mainSku || ""}
            fieldErrors={state.fieldErrors}
          />

          <FormSection title="Arama motoru bilgileri" description="Storefront ürün sayfasında kullanılacak başlık ve açıklamayı tanımlayın.">
            <div className="grid gap-4">
              <Field label="SEO başlığı" name="seoTitle" defaultValue={product?.seoTitle || ""} maxLength={250} error={fieldError("seoTitle")} />
              <label className="block text-sm font-medium text-foreground">
                SEO açıklaması
                <textarea name="seoDescription" defaultValue={product?.seoDescription || ""} maxLength={500} rows={4} className={`${inputClass} resize-y py-3`} aria-invalid={Boolean(fieldError("seoDescription"))} />
                <FieldError messages={fieldError("seoDescription")} />
              </label>
            </div>
          </FormSection>
        </div>

        <aside className="space-y-6 lg:sticky lg:top-24">
          <FormSection title="Durum">
            <label className="block text-sm font-medium text-foreground">
              Ürün durumu
              <select name="status" defaultValue={product?.status ?? 0} className={inputClass}>
                {productStatusOptions.map((status) => <option key={status.value} value={status.value}>{status.label}</option>)}
              </select>
              <FieldError messages={fieldError("status")} />
            </label>
            <div className="mt-4 space-y-2">
              <CheckField name="isFeatured" label="Öne çıkar" defaultChecked={product?.isFeatured ?? false} />
            </div>
          </FormSection>

          <FormSection title="Organizasyon">
            <div className="space-y-4">
              <Field label="Ürün tipi" name="type" defaultValue={product?.typeName || ""} maxLength={150} help="Adla gönderilir; yoksa API oluşturur." error={fieldError("type")} />
              <label className="block text-sm font-medium text-foreground">
                Marka
                <select name="brandId" defaultValue={product?.brandId || ""} className={inputClass}>
                  <option value="">Marka yok</option>
                  {options.brands.map((brand) => <option key={brand.id} value={brand.id}>{brand.name}</option>)}
                </select>
                <FieldError messages={fieldError("brandId")} />
              </label>
              <label className="block text-sm font-medium text-foreground">
                Vergi oranı
                <select name="taxRateId" defaultValue={product?.taxRateId || ""} className={inputClass} disabled={options.taxRatesUnavailable && !product?.taxRateId}>
                  <option value="">Vergi oranı yok</option>
                  {product?.taxRateId && !options.taxRates.some((rate) => rate.id === product.taxRateId) ? (
                    <option value={product.taxRateId}>{product.taxRateName || "Mevcut vergi oranı"}</option>
                  ) : null}
                  {options.taxRates.map((rate) => <option key={rate.id} value={rate.id}>{rate.name} (%{rate.rate})</option>)}
                </select>
                {options.taxRatesUnavailable ? <span className="mt-1 block text-xs font-normal leading-5 text-warning">Aktif vergi oranları yüklenemedi.</span> : null}
                <FieldError messages={fieldError("taxRateId")} />
              </label>
              <TagEditor
                initialTags={product?.tags.map((tag) => tag.name) || []}
                error={fieldError("tags")}
                onTagsChange={() => {
                  setDirty(true);
                  setBaseChanged(true);
                }}
              />
              {mode === "create" ? (
                <Field label="Koleksiyonlar" name="collections" defaultValue="" help="Virgülle ayırın; adla gönderilir." error={fieldError("collections")} />
              ) : (
                <p className="rounded-lg border border-border bg-surface-subtle p-3 text-xs leading-5 text-muted">Koleksiyon ilişkileri ürün detay DTO’sunda dönmediği için mevcut ilişkileri yanlışlıkla silmemek adına bu düzenleme formunda değiştirilmez.</p>
              )}
              <Field label="Görüntüleme sırası" name="displayOrder" defaultValue={String(product?.displayOrder ?? 0)} type="number" min="0" step="1" error={fieldError("displayOrder")} />
            </div>
          </FormSection>

          <div className="rounded-xl border border-primary/25 bg-primary-soft p-4">
            <p className="text-sm font-semibold text-foreground">Kayıt sınırı</p>
            <p className="mt-1 text-xs leading-5 text-muted">Ürün, durum, varyant ve görsel işlemleri ayrı API çağrıları olabilir. Kısmi başarı durumunda ürün kimliği korunur.</p>
          </div>
        </aside>
      </div>

      <div className="sticky bottom-0 z-[5] mt-6 flex flex-col gap-3 border-t border-border bg-page/95 px-1 py-4 sm:flex-row sm:items-center sm:justify-end">
        <Link href="/products" className="inline-flex min-h-11 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle">Vazgeç</Link>
        <SaveButton
          label={mode === "create" ? "Ürünü oluştur" : "Değişiklikleri kaydet"}
          disabled={mode === "edit" && !dirty}
        />
      </div>
    </form>
  );
}

// Burada anlamlı ürün alanlarını ağır kart tekrarına girmeden aynı bölüm yüzeyinde topluyorum.
function FormSection({ title, description, children }: { title: string; description?: string; children: React.ReactNode }) {
  return (
    <section className="rounded-xl border border-border bg-surface-strong p-4 sm:p-5">
      <div className="mb-4">
        <h2 className="text-base font-semibold text-foreground">{title}</h2>
        {description ? <p className="mt-1 text-sm leading-5 text-muted">{description}</p> : null}
      </div>
      {children}
    </section>
  );
}

// Burada standart ürün alanlarının kalıcı etiket, açıklama ve hata ilişkisini kuruyorum.
function Field({ label, name, defaultValue, type = "text", maxLength, min, step, required, help, error }: {
  label: string; name: string; defaultValue: string; type?: string; maxLength?: number; min?: string; step?: string; required?: boolean; help?: string; error?: string[];
}) {
  const helpId = `${name}-help`;
  const errorId = `${name}-error`;
  return (
    <label className="block text-sm font-medium text-foreground">
      {label}{required ? " *" : ""}
      <input name={name} type={type} defaultValue={defaultValue} maxLength={maxLength} min={min} step={step} required={required} className={inputClass} aria-invalid={Boolean(error)} aria-describedby={error ? errorId : help ? helpId : undefined} />
      {help ? <span id={helpId} className="mt-1 block text-xs font-normal leading-5 text-muted">{help}</span> : null}
      <FieldError id={errorId} messages={error} />
    </label>
  );
}

// Burada boolean ürün tercihlerini metin etiketiyle erişilebilir checkbox olarak sunuyorum.
function CheckField({ name, label, defaultChecked }: { name: string; label: string; defaultChecked: boolean }) {
  return (
    <label className="flex min-h-10 items-center gap-2 text-sm font-medium text-foreground">
      <input type="checkbox" name={name} defaultChecked={defaultChecked} className="size-4 accent-primary" />
      {label}
    </label>
  );
}

// Burada form alanı hata mesajını ilgili kontrolün hemen altında görünür tutuyorum.
function FieldError({ id, messages }: { id?: string; messages?: string[] }) {
  return messages ? <span id={id} className="mt-1 block text-xs font-semibold text-danger">{messages.join(" ")}</span> : null;
}

// Burada değişiklik yokken kaydı kapatıyor, gönderim sırasında çift tıklamayı engelleyip durumu metinle bildiriyorum.
function SaveButton({ label, disabled = false }: { label: string; disabled?: boolean }) {
  const { pending } = useFormStatus();
  const isDisabled = pending || disabled;
  return (
    <button type="submit" disabled={isDisabled} aria-disabled={isDisabled} className="inline-flex min-h-11 min-w-44 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:bg-muted disabled:text-white/80">
      {pending ? "Kaydediliyor…" : label}
    </button>
  );
}
