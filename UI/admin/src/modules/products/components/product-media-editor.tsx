"use client";

/* eslint-disable @next/next/no-img-element */

import { useEffect, useRef, useState } from "react";
import { validateProductImageFile } from "@/modules/products/cloudinary-upload";
import {
  MAX_PRODUCT_IMAGES,
  type ProductMediaDraft,
  type ProductMediaDraftItem,
} from "@/modules/products/product-media";
import type { ProductImage } from "@/modules/products/types";

type LocalMedia = ProductMediaDraftItem & {
  previewUrl: string;
};

type ProductMediaEditorProps = {
  images: ProductImage[];
  disabled?: boolean;
  onDirty: () => void;
  onDraftChange: (draft: ProductMediaDraft) => void;
};

// Burada kayıtlı görsellerle yeni dosyaları aynı kontrollü medya alanında yönetiyorum.
export function ProductMediaEditor({ images, disabled = false, onDirty, onDraftChange }: ProductMediaEditorProps) {
  const visibleExistingImages = images.slice(0, MAX_PRODUCT_IMAGES);
  const initialMainImage = visibleExistingImages.find((image) => image.isMain) || visibleExistingImages[0];
  const [localMedia, setLocalMedia] = useState<LocalMedia[]>([]);
  const [mainKey, setMainKey] = useState<string | null>(initialMainImage ? `existing-${initialMainImage.id}` : null);
  const [message, setMessage] = useState<string>();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const previewUrlsRef = useRef(new Set<string>());
  const totalCount = visibleExistingImages.length + localMedia.length;

  // Burada üst formun yükleme sırasında kullanacağı dosya ve ana görsel seçimini güncel tutuyorum.
  useEffect(() => {
    onDraftChange({
      localMedia: localMedia.map(({ key, file }) => ({ key, file })),
      mainKey,
    });
  }, [localMedia, mainKey, onDraftChange]);

  // Burada bileşen kapandığında tarayıcıda ürettiğim geçici önizleme URL'lerini serbest bırakıyorum.
  useEffect(() => {
    const previewUrls = previewUrlsRef.current;
    return () => previewUrls.forEach((url) => URL.revokeObjectURL(url));
  }, []);

  // Burada dosyaları tür, boyut ve toplam adet sınırından geçirip ilk yeni ürün görselini otomatik ana yapıyorum.
  const addFiles = (files: FileList | null) => {
    if (!files || disabled) return;
    const availableSlots = MAX_PRODUCT_IMAGES - totalCount;
    const validFiles: File[] = [];
    const errors: string[] = [];

    for (const file of Array.from(files)) {
      const validationError = validateProductImageFile(file);
      if (validationError) errors.push(`${file.name}: ${validationError}`);
      else validFiles.push(file);
    }

    const acceptedFiles = validFiles.slice(0, availableSlots);
    const nextMedia = acceptedFiles.map((file) => {
      const previewUrl = URL.createObjectURL(file);
      previewUrlsRef.current.add(previewUrl);
      return { key: `local-${crypto.randomUUID()}`, file, previewUrl };
    });

    if (nextMedia.length > 0) {
      setLocalMedia((current) => [...current, ...nextMedia]);
      setMainKey((current) => current || nextMedia[0].key);
      onDirty();
    }

    if (validFiles.length > availableSlots) errors.push(`En fazla ${MAX_PRODUCT_IMAGES} ürün görseli ekleyebilirsiniz.`);
    setMessage(errors[0]);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  // Burada yerel önizlemeyi kaldırıp ana seçimi kalan ilk uygun görsele taşıyorum.
  const removeLocalMedia = (key: string) => {
    if (disabled) return;
    const removing = localMedia.find((item) => item.key === key);
    if (removing) {
      URL.revokeObjectURL(removing.previewUrl);
      previewUrlsRef.current.delete(removing.previewUrl);
    }

    const remainingLocalMedia = localMedia.filter((item) => item.key !== key);
    setLocalMedia(remainingLocalMedia);
    if (mainKey === key) {
      const firstExisting = visibleExistingImages.find((image) => image.isMain) || visibleExistingImages[0];
      setMainKey(firstExisting ? `existing-${firstExisting.id}` : remainingLocalMedia[0]?.key || null);
    }
    setMessage(undefined);
    onDirty();
  };

  // Burada görsel seçimini değişiklik olarak işaretleyip yalnız seçilen ana görsel anahtarını tutuyorum.
  const selectMain = (key: string) => {
    if (disabled || key === mainKey) return;
    setMainKey(key);
    onDirty();
  };

  return (
    <section aria-labelledby="product-media-title" aria-busy={disabled} className="rounded-xl border border-border bg-surface-strong p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="product-media-title" className="text-base font-semibold text-foreground">Medya</h2>
          <p className="mt-0.5 text-xs leading-5 text-muted">JPG, PNG veya WebP biçiminde en fazla 10 görsel; her dosya en fazla 8 MB.</p>
        </div>
        <span className="rounded-md bg-surface-subtle px-2 py-1 text-xs font-bold tabular-nums text-muted">{totalCount}/{MAX_PRODUCT_IMAGES}</span>
      </div>

      <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-4 xl:grid-cols-6">
        {visibleExistingImages.map((image) => {
          const key = `existing-${image.id}`;
          return (
            <MediaCard
              key={key}
              mediaKey={key}
              src={image.imageUrl}
              alt={image.altText || "Ürün görseli"}
              label={image.altText || "Kayıtlı görsel"}
              isMain={mainKey === key}
              disabled={disabled}
              onSelectMain={selectMain}
            />
          );
        })}

        {localMedia.map((item) => (
          <MediaCard
            key={item.key}
            mediaKey={item.key}
            src={item.previewUrl}
            alt={item.file.name}
            label={item.file.name}
            isMain={mainKey === item.key}
            disabled={disabled}
            onSelectMain={selectMain}
            onRemove={removeLocalMedia}
          />
        ))}

        {totalCount < MAX_PRODUCT_IMAGES ? (
          <button
            type="button"
            disabled={disabled}
            onClick={() => fileInputRef.current?.click()}
            className="group aspect-square min-h-24 rounded-lg border-2 border-dashed border-border-strong bg-surface-subtle/45 text-muted transition-colors hover:border-primary hover:bg-primary-soft/40 hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:cursor-not-allowed disabled:opacity-60"
            aria-label="Ürün görseli ekle"
          >
            <span className="mx-auto flex size-8 items-center justify-center rounded-full border border-border-strong bg-surface-strong text-xl font-light leading-none transition-colors group-hover:border-primary">+</span>
            <span className="mt-1.5 block text-xs font-bold">Görsel ekle</span>
          </button>
        ) : null}
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        multiple
        disabled={disabled}
        className="sr-only"
        onChange={(event) => addFiles(event.target.files)}
        aria-label="Ürün görsellerini seç"
      />

      {message ? <p className="mt-3 text-sm font-semibold text-warning" role="alert">{message}</p> : null}
      {images.length > MAX_PRODUCT_IMAGES ? <p className="mt-3 text-xs text-warning">Mevcut kayıtların ilk 10 görseli gösteriliyor.</p> : null}
      {localMedia.length > 0 ? (
        <p className="mt-3 rounded-lg border border-blue-200 bg-blue-50 px-3 py-2 text-xs leading-5 text-blue-900">
          Yeni görseller ürünle birlikte kaydedilecek. Seçtiğiniz ana görsel ürün listesinde kullanılacak.
        </p>
      ) : null}
    </section>
  );
}

// Burada her görseli erişilebilir ana seçim ve isteğe bağlı kaldırma aksiyonlarıyla sunuyorum.
function MediaCard({
  mediaKey,
  src,
  alt,
  label,
  isMain,
  disabled,
  onSelectMain,
  onRemove,
}: {
  mediaKey: string;
  src: string;
  alt: string;
  label: string;
  isMain: boolean;
  disabled: boolean;
  onSelectMain: (key: string) => void;
  onRemove?: (key: string) => void;
}) {
  const [failed, setFailed] = useState(false);

  return (
    <article className={`group relative aspect-square min-h-24 overflow-hidden rounded-lg border-2 bg-surface-subtle transition-colors ${isMain ? "border-primary ring-2 ring-primary/15" : "border-border hover:border-border-strong"}`}>
      {failed ? (
        <span className="flex size-full items-center justify-center px-2 text-center text-xs font-semibold text-muted">Görsel açılamadı</span>
      ) : (
        <img src={src} alt={alt} onError={() => setFailed(true)} className="size-full object-cover" />
      )}
      <div className="absolute inset-x-0 bottom-0 flex items-end justify-between gap-2 bg-black/65 p-2 text-white">
        <span className="min-w-0 truncate text-[11px] font-semibold">{isMain ? "Ana görsel" : label}</span>
        {!isMain ? (
          <button type="button" disabled={disabled} onClick={() => onSelectMain(mediaKey)} className="shrink-0 rounded bg-white/95 px-2 py-1 text-[10px] font-bold text-slate-900 hover:bg-white disabled:opacity-60">
            Ana yap
          </button>
        ) : null}
      </div>
      {onRemove ? (
        <button
          type="button"
          disabled={disabled}
          onClick={() => onRemove(mediaKey)}
          aria-label={`${label} görselini kaldır`}
          className="absolute right-2 top-2 flex size-7 items-center justify-center rounded-full bg-black/70 text-sm font-bold text-white hover:bg-black disabled:opacity-60"
        >
          ×
        </button>
      ) : null}
    </article>
  );
}
