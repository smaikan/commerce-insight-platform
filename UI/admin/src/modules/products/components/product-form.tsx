"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useActionState, useCallback, useEffect, useRef, useState } from "react";
import { commitProductMediaAction, createProductAction, updateProductAction } from "@/modules/products/actions";
import { uploadProductImages } from "@/modules/products/cloudinary-upload";
import type { CloudinaryProductAsset, ProductMediaDraft } from "@/modules/products/product-media";
import { productStatusOptions } from "@/modules/products/query";
import {
  initialProductActionState,
  type Product,
  type ProductActionState,
  type ProductFormOptions,
  type ProductImage,
  type ProductStatus,
} from "@/modules/products/types";
import { VariantEditor } from "@/modules/products/components/variant-editor";
import { editableVariantRevision } from "@/modules/products/variant-editing";
import { isProductActionAwaitingResult } from "@/modules/products/product-save-state";
import { ProductMediaEditor } from "@/modules/products/components/product-media-editor";
import { TagEditor } from "@/modules/products/components/tag-editor";
import { CollectionSelector } from "@/modules/products/components/collection-selector";

const inputClass = "mt-1 min-h-11 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground sm:min-h-9";

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
  onDirtyChange,
}: {
  mode: "create" | "edit";
  product?: Product;
  images?: ProductImage[];
  options: ProductFormOptions;
  onDirtyChange?: (dirty: boolean) => void;
}) {
  const router = useRouter();
  const action = mode === "create" ? createProductAction : updateProductAction;
  const [state, formAction, actionPending] = useActionState(action, initialProductActionState);
  const [dirty, setDirty] = useState(false);
  const [baseChanged, setBaseChanged] = useState(mode === "create");
  const [selectedStatus, setSelectedStatus] = useState<ProductStatus>(product?.status ?? 0);
  const [mediaDraft, setMediaDraft] = useState<ProductMediaDraft>({ localMedia: [], mainKey: null, orderedKeys: [] });
  const [mediaPhase, setMediaPhase] = useState<"idle" | "uploading" | "registering" | "error">("idle");
  const [mediaMessage, setMediaMessage] = useState<string>();
  const [mediaEditorRevision, setMediaEditorRevision] = useState(0);
  const [deletedVariantIds, setDeletedVariantIds] = useState<Set<string>>(() => new Set());
  const [variantMutationMessage, setVariantMutationMessage] = useState<string>();
  const handledCompletionTokenRef = useRef<string | undefined>(undefined);
  const uploadedAssetsRef = useRef(new Map<string, CloudinaryProductAsset>());
  const committedKeysRef = useRef(new Set<string>());
  const [actionStateAtSubmit, setActionStateAtSubmit] = useState<ProductActionState>(state);
  const awaitingActionResult = isProductActionAwaitingResult(actionPending, state, actionStateAtSubmit);
  const mediaPending = mediaPhase === "uploading" || mediaPhase === "registering";

  // Burada ürün taslağının değişiklik durumunu hem yerel korumalara hem aynı sayfadaki bağımsız işlemlere bildiriyorum.
  const setFormDirty = useCallback((nextDirty: boolean) => {
    setDirty(nextDirty);
    onDirtyChange?.(nextDirty);
  }, [onDirtyChange]);

  // Burada medya editörünün dosya ve ana seçim taslağını form kaydetme akışı için saklıyorum.
  const handleMediaDraftChange = useCallback((draft: ProductMediaDraft) => {
    setMediaDraft(draft);
  }, []);

  // Burada legacy varyant şemasının canonical birleşik şemaya alınmasını kaydedilebilir değişiklik olarak işaretliyorum.
  const handleVariantNormalizationNeeded = useCallback(() => {
    setFormDirty(true);
  }, [setFormDirty]);

  // Burada başarılı kayıttan sonra yerel önizlemeleri ve bekleme durumunu temizleyip veritabanındaki güncel görselleri yeniden okutuyorum.
  const finishSuccessfulSave = useCallback((productId: string) => {
    uploadedAssetsRef.current.clear();
    committedKeysRef.current.clear();
    setMediaMessage(undefined);
    setMediaPhase("idle");
    setMediaDraft({ localMedia: [], mainKey: null, orderedKeys: [] });
    setMediaEditorRevision((current) => current + 1);
    setFormDirty(false);
    router.replace(`/products/${encodeURIComponent(productId)}?${mode === "create" ? "created" : "saved"}=1`);
    router.refresh();
  }, [mode, router, setFormDirty]);

  // Burada ürün kaydı oluştuktan sonra yeni dosyaları Cloudinary'ye yükleyip API görsel kayıtlarını tamamlıyorum.
  const completeMediaSave = useCallback(async (productId: string) => {
    try {
      setMediaMessage(undefined);
      const uncommittedMedia = mediaDraft.localMedia.filter((item) => !committedKeysRef.current.has(item.key));
      const missingUploads = uncommittedMedia.filter((item) => !uploadedAssetsRef.current.has(item.key));

      if (missingUploads.length > 0) {
        setMediaPhase("uploading");
        const batch = await uploadProductImages(missingUploads, productId);
        batch.uploaded.forEach((asset) => uploadedAssetsRef.current.set(asset.clientKey, asset));
        if (batch.failed.length > 0) {
          const firstFailure = batch.failed[0];
          setMediaPhase("error");
          setMediaMessage(`${firstFailure.fileName}: ${firstFailure.message}${batch.failed.length > 1 ? ` (${batch.failed.length} dosya başarısız)` : ""}`);
          return;
        }
      }

      const selectedExistingId = mediaDraft.mainKey?.startsWith("existing-")
        ? mediaDraft.mainKey.slice("existing-".length)
        : undefined;
      const selectedExisting = selectedExistingId ? images.find((image) => image.id === selectedExistingId) : undefined;
      const orderByKey = new Map(mediaDraft.orderedKeys.map((key, index) => [key, index]));
      const existingImages = images.map((image) => ({
        id: image.id,
        displayOrder: orderByKey.get(`existing-${image.id}`) ?? image.displayOrder,
      }));
      const newImages = mediaDraft.localMedia
        .map((item) => {
          if (committedKeysRef.current.has(item.key)) return null;
          const asset = uploadedAssetsRef.current.get(item.key);
          return asset ? {
            ...asset,
            displayOrder: orderByKey.get(item.key) ?? mediaDraft.orderedKeys.length,
            isMain: mediaDraft.mainKey === item.key,
          } : null;
        })
        .filter((image): image is NonNullable<typeof image> => image !== null);
      const existingOrderChanged = existingImages.some((orderedImage) =>
        images.find((image) => image.id === orderedImage.id)?.displayOrder !== orderedImage.displayOrder,
      );
      const shouldUpdateExistingMain = Boolean(selectedExisting && !selectedExisting.isMain);

      if (newImages.length === 0 && !shouldUpdateExistingMain && !existingOrderChanged) {
        finishSuccessfulSave(productId);
        return;
      }

      setMediaPhase("registering");
      const result = await commitProductMediaAction({
        productId,
        mainExistingImageId: shouldUpdateExistingMain ? selectedExistingId : undefined,
        existingImages,
        newImages,
      });
      result.committedClientKeys.forEach((key) => committedKeysRef.current.add(key));

      if (result.status !== "success") {
        setMediaPhase("error");
        setMediaMessage(`${result.message || "Görseller ürüne bağlanamadı."}${result.traceId ? ` Takip kodu: ${result.traceId}` : ""}`);
        return;
      }

      finishSuccessfulSave(productId);
    } catch {
      setMediaPhase("error");
      setMediaMessage("Görsel kaydı beklenmeyen bir bağlantı hatası nedeniyle tamamlanamadı. Yalnız eksik adımı yeniden deneyin.");
    }
  }, [finishSuccessfulSave, images, mediaDraft]);

  // Burada her başarılı form gönderimini benzersiz tamamlanma anahtarıyla yalnız bir kez medya aşamasına geçiriyorum.
  useEffect(() => {
    if (
      state.status !== "success"
      || !state.productId
      || !state.completionToken
      || handledCompletionTokenRef.current === state.completionToken
    ) return;
    handledCompletionTokenRef.current = state.completionToken;
    void completeMediaSave(state.productId);
  }, [completeMediaSave, state.completionToken, state.productId, state.status]);

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
  const savedVariantEditorState = state.savedVariantEditorState;
  const editorVariants = (savedVariantEditorState?.variants || product?.variants || [])
    .filter((variant) => !deletedVariantIds.has(variant.id));
  const editorHasVariants = savedVariantEditorState?.hasVariants ?? product?.hasVariants ?? false;
  const editorMainSku = savedVariantEditorState?.mainSku ?? product?.mainSku ?? "";

  // Burada yalnız temel ürün DTO'suna ait bir kontrol değiştiğinde temel güncelleme çağrısını etkinleştiriyorum.
  const handleFormChange = (event: React.FormEvent<HTMLFormElement>) => {
    setFormDirty(true);
    const control = event.target;
    if (
      (control instanceof HTMLInputElement || control instanceof HTMLTextAreaElement || control instanceof HTMLSelectElement)
      && baseProductFieldNames.has(control.name)
    ) {
      setBaseChanged(true);
    }
  };

  // Burada Server Action sonrasındaki yerel form sıfırlamasının durum seçimini eski değere döndürmesini engelliyorum.
  const handleStatusChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    setSelectedStatus(Number(event.target.value) as ProductStatus);
  };

  return (
    <form
      action={formAction}
      onSubmit={() => setActionStateAtSubmit(state)}
      onChange={handleFormChange}
      aria-busy={awaitingActionResult || mediaPending}
      className="pb-4"
    >
      {baseChanged ? <input type="hidden" name="baseChanged" value="on" /> : null}
      {product ? (
        <>
          <input type="hidden" name="productId" value={product.id} />
          <input type="hidden" name="originalStatus" value={product.status} />
          <input type="hidden" name="originalIsFeatured" value={String(product.isFeatured)} />
          <input type="hidden" name="originalHasVariants" value={String(product.hasVariants)} />
        </>
      ) : null}

      {state.status === "error" || state.status === "partial" ? (
        <div className={`mb-5 rounded-xl border px-4 py-3 text-sm ${state.status === "partial" ? "border-amber-300 bg-amber-50 text-amber-900" : "border-red-300 bg-red-50 text-red-900"}`} role="alert">
          <p className="font-semibold">{state.status === "partial" ? "Kısmi kayıt" : "Kayıt tamamlanamadı"}</p>
          <p className="mt-1 leading-6">{state.message}</p>
          {state.completedOperations?.length ? (
            <div className="mt-3">
              <p className="font-semibold">Kaydedilenler</p>
              <ul className="mt-1 list-disc space-y-1 pl-5">
                {state.completedOperations.map((operation) => <li key={operation}>{operation}</li>)}
              </ul>
            </div>
          ) : null}
          {state.failedOperations?.length ? (
            <div className="mt-3">
              <p className="font-semibold">Tamamlanamayanlar</p>
              <ul className="mt-1 list-disc space-y-1 pl-5">
                {state.failedOperations.map((operation) => <li key={operation}>{operation}</li>)}
              </ul>
            </div>
          ) : null}
          {state.traceId ? <p className="mt-1 text-xs">Takip kodu: {state.traceId}</p> : null}
          {state.productId ? (
            <Link href={state.reloadHref || `/products/${state.productId}`} className="mt-2 inline-flex font-semibold underline">
              {state.reloadHref ? "Güncel kaydı yükle" : "Kaydedilen ürünü aç"}
            </Link>
          ) : null}
        </div>
      ) : null}

      <div className="grid items-start gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <div className="space-y-4">
          <FormSection title="Temel bilgiler" description="Ürünün katalog kimliğini ve açıklamasını tanımlayın.">
            <div className="grid gap-3">
              <Field label="Ürün başlığı" name="title" defaultValue={product?.title || ""} maxLength={250} required error={fieldError("title")} />
              <label className="block text-sm font-medium text-foreground">
                Açıklama
                <textarea name="description" defaultValue={product?.description || ""} maxLength={4000} rows={5} className={`${inputClass} resize-y py-2.5`} aria-invalid={Boolean(fieldError("description"))} />
                <FieldError messages={fieldError("description")} />
              </label>
            </div>
          </FormSection>

          <ProductMediaEditor
            key={`${mediaEditorRevision}:${images.map((image) => image.id).join(",")}`}
            productId={product?.id}
            images={images}
            disabled={mediaPhase === "uploading" || mediaPhase === "registering"}
            onDirty={() => setFormDirty(true)}
            onDraftChange={handleMediaDraftChange}
          />

          <VariantEditor
            key={`${editorHasVariants}:${editorMainSku}:${editableVariantRevision(editorVariants)}`}
            variants={editorVariants}
            mode={mode}
            productId={product?.id}
            initialHasVariants={editorHasVariants}
            initialMainSku={editorMainSku}
            fieldErrors={state.fieldErrors}
            deletionDisabled={dirty || awaitingActionResult || mediaPhase !== "idle"}
            onVariantDeleted={(variantId, message) => {
              setDeletedVariantIds((current) => new Set(current).add(variantId));
              setVariantMutationMessage(message);
              router.refresh();
            }}
            onNormalizationNeeded={handleVariantNormalizationNeeded}
          />
          {variantMutationMessage ? (
            <p className="rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm font-semibold text-green-900" role="status">
              {variantMutationMessage}
            </p>
          ) : null}

          <FormSection title="Arama motoru bilgileri" description="Storefront ürün sayfasında kullanılacak başlık ve açıklamayı tanımlayın.">
            <div className="grid gap-3">
              <Field label="SEO başlığı" name="seoTitle" defaultValue={product?.seoTitle || ""} maxLength={250} error={fieldError("seoTitle")} />
              <label className="block text-sm font-medium text-foreground">
                SEO açıklaması
                <textarea name="seoDescription" defaultValue={product?.seoDescription || ""} maxLength={500} rows={3} className={`${inputClass} resize-y py-2.5`} aria-invalid={Boolean(fieldError("seoDescription"))} />
                <FieldError messages={fieldError("seoDescription")} />
              </label>
              <Field label="Ürün URL'si" name="url" defaultValue={product?.url || ""} maxLength={250} help="Boş bırakırsanız ürün adına göre oluşturulur." error={fieldError("url")} />
            </div>
          </FormSection>
        </div>

        <aside className="space-y-4 lg:sticky lg:top-20">
          <FormSection title="Durum">
            <label className="block text-sm font-medium text-foreground">
              Ürün durumu
              <select name="status" value={selectedStatus} onChange={handleStatusChange} className={inputClass}>
                {productStatusOptions.map((status) => <option key={status.value} value={status.value}>{status.label}</option>)}
              </select>
              <FieldError messages={fieldError("status")} />
            </label>
            <div className="mt-3 space-y-2">
              <CheckField name="isFeatured" label="Öne çıkar" defaultChecked={product?.isFeatured ?? false} />
            </div>
          </FormSection>

          <FormSection title="Organizasyon">
            <div className="space-y-3">
              <Field label="Ürün tipi" name="type" defaultValue={product?.typeName || ""} maxLength={150} help="Yeni bir ad girerek ürün tipi ekleyebilirsiniz." error={fieldError("type")} />
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
                  setFormDirty(true);
                  setBaseChanged(true);
                }}
              />
              {mode === "create" ? (
                <CollectionSelector
                  collections={options.collections}
                  unavailable={options.collectionsUnavailable}
                  error={fieldError("collections")}
                  onCollectionsChange={() => {
                    setFormDirty(true);
                    setBaseChanged(true);
                  }}
                />
              ) : (
                <CollectionMemberships product={product} />
              )}
              <Field label="Görüntüleme sırası" name="displayOrder" defaultValue={String(product?.displayOrder ?? 0)} type="number" min="0" step="1" error={fieldError("displayOrder")} />
            </div>
          </FormSection>

        </aside>
      </div>

      <div className="sticky bottom-0 z-[5] mt-4 flex flex-col gap-2 border-t border-border bg-page/95 px-1 py-3 sm:flex-row sm:items-center sm:justify-end">
        {mediaPhase !== "idle" ? (
          <div className="sm:mr-auto" role={mediaPhase === "error" ? "alert" : "status"} aria-live="polite">
            <p className={`text-sm font-semibold ${mediaPhase === "error" ? "text-danger" : "text-primary"}`}>
              {mediaPhase === "uploading"
                ? "Görseller medya hizmetine yükleniyor…"
                : mediaPhase === "registering"
                  ? "Yüklenen görseller ürüne bağlanıyor…"
                  : mediaMessage}
            </p>
            {mediaPhase === "error" && state.productId ? (
              <button type="button" onClick={() => void completeMediaSave(state.productId as string)} className="mt-1 text-sm font-semibold text-primary underline underline-offset-2">
                Yalnız eksik adımı yeniden dene
              </button>
            ) : null}
          </div>
        ) : null}
        <Link href="/products" className="inline-flex min-h-11 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle">Vazgeç</Link>
        <SaveButton
          label={mode === "create" ? "Ürünü oluştur" : "Değişiklikleri kaydet"}
          disabled={(mode === "edit" && !dirty) || mediaPhase !== "idle"}
          actionPending={awaitingActionResult}
          mediaPhase={mediaPhase}
        />
      </div>
    </form>
  );
}

// Burada PDF referansındaki ürün organizasyonu alanına benzer biçimde koleksiyonları türlerine göre rozetle gösteriyorum.
function CollectionMemberships({ product }: { product?: Product }) {
  return <div><p className="text-sm font-medium text-foreground">Koleksiyonlar</p><div className="mt-1 rounded-lg border border-border bg-surface-subtle p-2.5">{product?.collections.length ? <div className="flex flex-wrap gap-1.5">{product.collections.map((collection) => <span key={collection.id} className="rounded-md border border-slate-200 bg-slate-100 px-2 py-1 text-xs font-semibold text-slate-700">{collection.name}</span>)}</div> : <p className="text-xs leading-5 text-muted">Bu ürün henüz manuel bir koleksiyona eklenmemiş.</p>}</div><p className="mt-1 text-xs leading-5 text-muted">Manuel üyelikler gri, otomatik üyelikler API sözleşmesi geldiğinde mavi gösterilecek.</p></div>;
}

// Burada anlamlı ürün alanlarını ağır kart tekrarına girmeden aynı bölüm yüzeyinde topluyorum.
function FormSection({ title, description, children }: { title: string; description?: string; children: React.ReactNode }) {
  return (
    <section className="rounded-xl border border-border bg-surface-strong p-4">
      <div className="mb-3">
        <h2 className="text-base font-semibold text-foreground">{title}</h2>
        {description ? <p className="mt-0.5 text-xs leading-5 text-muted">{description}</p> : null}
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
function SaveButton({
  label,
  disabled = false,
  actionPending = false,
  mediaPhase = "idle",
}: {
  label: string;
  disabled?: boolean;
  actionPending?: boolean;
  mediaPhase?: "idle" | "uploading" | "registering" | "error";
}) {
  const externalPending = mediaPhase === "uploading" || mediaPhase === "registering";
  const isPending = actionPending || externalPending;
  const isDisabled = isPending || disabled;
  const pendingLabel = actionPending
    ? "Ürün kaydediliyor…"
    : mediaPhase === "uploading"
      ? "Görseller yükleniyor…"
      : "Görseller bağlanıyor…";
  return (
    <button type="submit" disabled={isDisabled} aria-disabled={isDisabled} className="inline-flex min-h-11 min-w-44 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:bg-muted disabled:text-white/80">
      {isPending ? pendingLabel : label}
    </button>
  );
}
