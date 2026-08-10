"use client";

/* eslint-disable @next/next/no-img-element */

import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import {
  attachCollectionImageAction,
  createManualCollectionAction,
  updateManualCollectionAction,
} from "@/modules/collections/actions";
import {
  uploadCloudinaryAsset,
  validateImageFile,
  type CloudinaryAsset,
} from "@/lib/cloudinary/browser-upload";
import {
  initialCollectionActionState,
  type Collection,
  type CollectionActionState,
} from "@/modules/collections/types";

type CollectionFormProps = {
  collection?: Collection;
  mode: "create" | "edit";
};

type MediaMode = "keep" | "remove" | "replace";
type MediaPhase = "idle" | "uploading" | "registering" | "error";

// Burada manuel koleksiyonun içerik ve tek görsel kaydını kısmi başarıyı koruyan tek form akışında yönetiyorum.
export function CollectionForm({ collection, mode }: CollectionFormProps) {
  const router = useRouter();
  const isCreate = mode === "create";
  const fileInputRef = useRef<HTMLInputElement>(null);
  const previewUrlRef = useRef<string | undefined>(undefined);
  const uploadedAssetRef = useRef<CloudinaryAsset | undefined>(undefined);
  const [selectedFile, setSelectedFile] = useState<File>();
  const [previewUrl, setPreviewUrl] = useState(collection?.imageUrl || "");
  const [mediaMode, setMediaMode] = useState<MediaMode>("keep");
  const [mediaPhase, setMediaPhase] = useState<MediaPhase>("idle");
  const [mediaMessage, setMediaMessage] = useState<string>();

  // Burada başarılı Cloudinary sonucunu tekrar denemelerde yeniden yüklemeden aynı koleksiyona bağlıyorum.
  async function uploadSelectedImage(file: File, collectionId: string): Promise<CloudinaryAsset | undefined> {
    if (uploadedAssetRef.current) return uploadedAssetRef.current;
    setMediaPhase("uploading");
    try {
      const asset = await uploadCloudinaryAsset({
        file,
        folder: `collections/${collectionId}`,
        resourceType: "image",
        tags: ["collection-image", `collection-${collectionId}`],
      });
      uploadedAssetRef.current = asset;
      return asset;
    } catch (error) {
      const message = error instanceof Error ? error.message : "Görsel kaydedilemedi.";
      setMediaPhase("error");
      setMediaMessage(message);
      return undefined;
    }
  }

  // Burada form submitini koleksiyon kaydı, Cloudinary yüklemesi ve URL bağlama adımlarına ayırıyorum.
  const submitCollection = async (
    previousState: CollectionActionState,
    formData: FormData,
  ): Promise<CollectionActionState> => {
    setMediaMessage(undefined);

    if (isCreate) {
      formData.set("imageMode", "keep");
      formData.delete("imageUrl");
      formData.delete("imagePublicId");
      const created = previousState.collectionId
        ? { status: "success" as const, collectionId: previousState.collectionId }
        : await createManualCollectionAction(previousState, formData);
      if (created.status !== "success" || !created.collectionId || !selectedFile) return created;

      const asset = await uploadSelectedImage(selectedFile, created.collectionId);
      if (!asset) {
        return {
          status: "partial",
          collectionId: created.collectionId,
          message: mediaMessage || "Koleksiyon oluşturuldu ancak görsel kaydedilemedi. Yalnız görsel adımını yeniden deneyin.",
        };
      }

      setMediaPhase("registering");
      const attached = await attachCollectionImageAction(created.collectionId, asset);
      setMediaPhase(attached.status === "success" ? "idle" : "error");
      if (attached.status !== "success") setMediaMessage(attached.message);
      return attached;
    }

    if (!collection) return { status: "error", message: "Koleksiyon kaydı bulunamadı." };
    formData.set("imageMode", mediaMode);
    if (mediaMode === "replace") {
      if (!selectedFile) return { status: "error", collectionId: collection.id, message: "Yüklenecek görsel bulunamadı." };
      const asset = await uploadSelectedImage(selectedFile, collection.id);
      if (!asset) return { status: "error", collectionId: collection.id, message: mediaMessage || "Görsel kaydedilemedi." };
      formData.set("imageUrl", asset.secureUrl);
      formData.set("imagePublicId", asset.publicId);
      formData.set("imageResourceType", asset.resourceType);
    } else {
      formData.delete("imageUrl");
      formData.delete("imagePublicId");
      formData.delete("imageResourceType");
    }

    setMediaPhase("registering");
    const updated = await updateManualCollectionAction(collection.id, previousState, formData);
    setMediaPhase(updated.status === "success" ? "idle" : "error");
    if (updated.status !== "success") setMediaMessage(updated.message);
    return updated;
  };

  const [state, formAction, pending] = useActionState(submitCollection, initialCollectionActionState);

  // Burada başarılı kayıttan sonra yetkili detayı yeniden okutup kalıcı sonucu URL durumuyla bildiriyorum.
  useEffect(() => {
    if (state.status !== "success" || !state.collectionId) return;
    router.replace(`/collections/${encodeURIComponent(state.collectionId)}?${isCreate ? "created" : "updated"}=1`);
    router.refresh();
  }, [isCreate, router, state.collectionId, state.status]);

  // Burada bileşen kapandığında tarayıcıdaki geçici görsel URL'sini serbest bırakıyorum.
  useEffect(() => () => {
    if (previewUrlRef.current) URL.revokeObjectURL(previewUrlRef.current);
  }, []);

  // Burada seçilen dosyayı ortak tip ve boyut kurallarından geçirip yerel önizlemeyi hazırlıyorum.
  const selectImage = (file?: File) => {
    if (!file) return;
    const validationError = validateImageFile(file);
    if (validationError) {
      setMediaMessage(validationError);
      setMediaPhase("error");
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }
    if (previewUrlRef.current) URL.revokeObjectURL(previewUrlRef.current);
    const nextPreview = URL.createObjectURL(file);
    previewUrlRef.current = nextPreview;
    uploadedAssetRef.current = undefined;
    setSelectedFile(file);
    setPreviewUrl(nextPreview);
    setMediaMode("replace");
    setMediaPhase("idle");
    setMediaMessage(undefined);
  };

  // Burada formdaki görseli kaldırıp mevcut kaydın ancak kaydetmeyle null olmasını sağlıyorum.
  const removeImage = () => {
    if (previewUrlRef.current) URL.revokeObjectURL(previewUrlRef.current);
    previewUrlRef.current = undefined;
    uploadedAssetRef.current = undefined;
    setSelectedFile(undefined);
    setPreviewUrl("");
    setMediaMode(collection?.imageUrl ? "remove" : "keep");
    setMediaPhase("idle");
    setMediaMessage(undefined);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const fieldError = (name: string) => state.fieldErrors?.[name];
  const formBusy = pending || mediaPhase === "uploading" || mediaPhase === "registering";

  return (
    <form action={formAction} className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_18rem]" aria-busy={formBusy}>
      <div className="space-y-4">
        <section className="rounded-xl border border-border bg-surface-strong p-4 sm:p-5" aria-labelledby="collection-details-title">
          <div className="border-b border-border pb-4">
            <h2 id="collection-details-title" className="text-base font-semibold text-foreground">Koleksiyon bilgileri</h2>
            <p className="mt-1 text-sm leading-5 text-muted">Müşterilerin göreceği adı, açıklamayı ve bağlantı adresini belirleyin.</p>
          </div>
          <div className="mt-5 grid gap-4">
            <Field label="Koleksiyon adı" name="name" defaultValue={collection?.name || ""} required maxLength={150} error={fieldError("name")} />
            <Field label="Bağlantı" name="url" defaultValue={collection?.url || ""} maxLength={200} help="Boş bırakırsanız API koleksiyon adına göre bir bağlantı üretir." error={fieldError("url")} />
            <label className="block text-sm font-medium text-foreground">
              Açıklama
              <textarea name="description" rows={7} maxLength={1000} defaultValue={collection?.description || ""} className="mt-1.5 w-full rounded-lg border border-border-strong bg-surface-strong px-3 py-2 text-sm leading-6 text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft" aria-invalid={Boolean(fieldError("description"))} />
              <span className="mt-1 block text-xs font-normal leading-5 text-muted">En fazla 1.000 karakter.</span>
              <FieldError messages={fieldError("description")} />
            </label>
          </div>
        </section>

        <section className="rounded-xl border border-border bg-surface-strong p-4 sm:p-5" aria-labelledby="collection-image-title">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h2 id="collection-image-title" className="text-base font-semibold text-foreground">Koleksiyon görseli</h2>
              <p className="mt-1 text-sm leading-5 text-muted">Storefront koleksiyon kartında kullanılacak tek görseli seçin.</p>
            </div>
            <span className="text-xs font-medium text-muted">JPG, PNG veya WebP · En fazla 8 MB</span>
          </div>

          <div className="mt-4 flex flex-col gap-4 sm:flex-row sm:items-start">
            <div className="flex aspect-[4/3] w-full max-w-72 items-center justify-center overflow-hidden rounded-lg border border-border bg-surface-subtle">
              {previewUrl ? <img src={previewUrl} alt="Koleksiyon görseli önizlemesi" className="size-full object-cover" /> : <ImagePlaceholder />}
            </div>
            <div className="flex flex-1 flex-wrap gap-2">
              <button type="button" disabled={formBusy} onClick={() => fileInputRef.current?.click()} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60">
                {previewUrl ? "Görseli değiştir" : "Görsel seç"}
              </button>
              {previewUrl ? <button type="button" disabled={formBusy} onClick={removeImage} className="inline-flex min-h-10 items-center rounded-lg px-3 text-sm font-semibold text-danger hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-60">Görseli kaldır</button> : null}
              <input ref={fileInputRef} type="file" accept="image/jpeg,image/png,image/webp" className="sr-only" disabled={formBusy} onChange={(event) => selectImage(event.target.files?.[0])} aria-label="Koleksiyon görseli seç" />
              <div className="w-full" aria-live="polite">
                {mediaPhase === "uploading" ? <p className="text-sm font-medium text-primary">Görsel kaydediliyor…</p> : null}
                {mediaPhase === "registering" ? <p className="text-sm font-medium text-primary">Koleksiyon güncelleniyor…</p> : null}
                {mediaMessage ? <p className="text-sm font-semibold text-danger" role="alert">{mediaMessage}</p> : null}
                <FieldError messages={fieldError("imageUrl")} />
              </div>
            </div>
          </div>
        </section>

        {state.status === "error" || state.status === "partial" ? (
          <div className={`rounded-xl border px-4 py-3 text-sm ${state.status === "partial" ? "border-amber-300 bg-amber-50 text-amber-900" : "border-red-300 bg-red-50 text-red-900"}`} role="alert">
            <p className="font-semibold">{state.status === "partial" ? "Koleksiyon oluşturuldu, görsel bekliyor" : "Kayıt tamamlanamadı"}</p>
            <p className="mt-1 leading-6">{state.message}</p>
            {state.traceId ? <p className="mt-1 text-xs">Takip kodu: {state.traceId}</p> : null}
          </div>
        ) : null}
      </div>

      <aside className="space-y-4 lg:sticky lg:top-20">
        <section className="rounded-xl border border-border bg-surface-strong p-4" aria-labelledby="collection-type-title">
          <h2 id="collection-type-title" className="text-base font-semibold text-foreground">Koleksiyon türü</h2>
          <div className="mt-3 rounded-lg border border-slate-200 bg-slate-50 p-3">
            <p className="text-sm font-semibold text-slate-800">Manuel</p>
            <p className="mt-1 text-xs leading-5 text-slate-600">Ürünleri ürün oluşturma veya düzenleme ekranından seçersiniz.</p>
          </div>
          <div className="mt-3 rounded-lg border border-border bg-surface-subtle p-3" aria-disabled="true">
            <div className="flex items-center justify-between gap-3"><p className="text-sm font-semibold text-muted">Otomatik</p><span className="text-[11px] font-semibold text-muted">Geliştirme aşamasında</span></div>
            <p className="mt-1 text-xs leading-5 text-muted">Koşul sözleşmesi API&apos;ye eklendiğinde etkinleştirilecek.</p>
          </div>
        </section>

        <section className="rounded-xl border border-border bg-surface-strong p-4" aria-labelledby="collection-order-title">
          <h2 id="collection-order-title" className="text-base font-semibold text-foreground">Sıralama</h2>
          <div className="mt-3"><Field label="Görüntüleme sırası" name="displayOrder" type="number" defaultValue={String(collection?.displayOrder ?? 0)} min="0" error={fieldError("displayOrder")} /></div>
          {isCreate ? <div className="mt-4 border-t border-border pt-3"><Checkbox name="isActive" label="Aktif" defaultChecked /><Checkbox name="isFeatured" label="Vitrinde öne çıkar" /></div> : <p className="mt-3 text-xs leading-5 text-muted">Aktiflik ve vitrin durumu ayrı kontrollerden yönetilir.</p>}
        </section>

        <button type="submit" disabled={formBusy} className="inline-flex min-h-11 w-full items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white outline-none hover:bg-primary-hover focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:bg-muted disabled:text-white/80">
          {formBusy ? "Kaydediliyor…" : state.status === "partial" ? "Görseli yeniden dene" : isCreate ? "Koleksiyonu oluştur" : "Değişiklikleri kaydet"}
        </button>
      </aside>
    </form>
  );
}

// Burada koleksiyon görseli bulunmadığında dekoratif olmayan sade bir yer tutucu gösteriyorum.
function ImagePlaceholder() {
  return <div className="text-center text-muted"><svg viewBox="0 0 24 24" className="mx-auto size-8 fill-none stroke-current stroke-[1.5]" aria-hidden="true"><path d="M4 5.5h16v13H4z" /><path d="m4 15 4-4 3 3 2-2 7 6.5" /><circle cx="15.5" cy="9" r="1.5" /></svg><p className="mt-2 text-xs font-medium">Görsel yok</p></div>;
}

// Burada formdaki tek satırlı alanları aynı etiket, yardım ve hata yapısında tutuyorum.
function Field({ label, name, defaultValue, type = "text", required = false, maxLength, min, help, error }: { label: string; name: string; defaultValue: string; type?: string; required?: boolean; maxLength?: number; min?: string; help?: string; error?: string[] }) {
  return <label className="block text-sm font-medium text-foreground">{label}{required ? " *" : ""}<input name={name} type={type} defaultValue={defaultValue} required={required} maxLength={maxLength} min={min} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft" aria-invalid={Boolean(error)} />{help ? <span className="mt-1 block text-xs font-normal leading-5 text-muted">{help}</span> : null}<FieldError messages={error} /></label>;
}

// Burada koleksiyon formu alan hatasını ilgili kontrolün hemen ardında okunabilir tutuyorum.
function FieldError({ messages }: { messages?: string[] }) {
  return messages?.length ? <span className="mt-1 block text-xs font-semibold text-danger">{messages.join(" ")}</span> : null;
}

// Burada başlangıç yayın durumlarını erişilebilir onay kutularıyla topluyorum.
function Checkbox({ name, label, defaultChecked = false }: { name: string; label: string; defaultChecked?: boolean }) {
  return <label className="flex min-h-10 items-center gap-2 text-sm font-medium text-foreground"><input name={name} type="checkbox" defaultChecked={defaultChecked} className="size-4 accent-primary" />{label}</label>;
}
