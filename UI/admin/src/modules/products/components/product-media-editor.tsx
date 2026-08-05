"use client";

/* eslint-disable @next/next/no-img-element */

import { useEffect, useRef, useState } from "react";
import type { ProductImage } from "@/modules/products/types";

type LocalMedia = {
  key: string;
  file: File;
  previewUrl: string;
};

const MAX_PRODUCT_IMAGES = 10;

// Burada mevcut ürün görselleriyle yerel dosya önizlemelerini en fazla on öğelik tek medya ızgarasında yönetiyorum.
export function ProductMediaEditor({ images }: { images: ProductImage[] }) {
  const visibleExistingImages = images.slice(0, MAX_PRODUCT_IMAGES);
  const initialMainImage = visibleExistingImages.find((image) => image.isMain) || visibleExistingImages[0];
  const [localMedia, setLocalMedia] = useState<LocalMedia[]>([]);
  const [mainKey, setMainKey] = useState<string | null>(initialMainImage ? `existing-${initialMainImage.id}` : null);
  const [message, setMessage] = useState<string>();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const previewUrlsRef = useRef(new Set<string>());
  const totalCount = visibleExistingImages.length + localMedia.length;

  // Burada bileşen kapandığında tarayıcıda ürettiğim tüm geçici önizleme URL'lerini serbest bırakıyorum.
  useEffect(() => {
    const previewUrls = previewUrlsRef.current;
    return () => previewUrls.forEach((url) => URL.revokeObjectURL(url));
  }, []);

  // Burada seçilen görsel dosyalarını kalan kapasite kadar ekleyip ilk görseli otomatik ana görsel yapıyorum.
  const addFiles = (files: FileList | null) => {
    if (!files) return;
    const availableSlots = MAX_PRODUCT_IMAGES - totalCount;
    const imageFiles = Array.from(files).filter((file) => file.type.startsWith("image/"));
    const acceptedFiles = imageFiles.slice(0, availableSlots);
    const nextMedia = acceptedFiles.map((file) => {
      const previewUrl = URL.createObjectURL(file);
      previewUrlsRef.current.add(previewUrl);
      return {
        key: `local-${crypto.randomUUID()}`,
        file,
        previewUrl,
      };
    });

    if (nextMedia.length > 0) {
      setLocalMedia((current) => [...current, ...nextMedia]);
      setMainKey((current) => current || nextMedia[0].key);
    }

    if (imageFiles.length > availableSlots) {
      setMessage(`En fazla ${MAX_PRODUCT_IMAGES} ürün görseli ekleyebilirsiniz.`);
    } else if (imageFiles.length !== files.length) {
      setMessage("Yalnızca görsel dosyaları seçilebilir.");
    } else {
      setMessage(undefined);
    }

    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  // Burada yalnız yerel önizlemeyi kaldırıp ana görsel seçimini kalan ilk görsele güvenli biçimde taşıyorum.
  const removeLocalMedia = (key: string) => {
    const removing = localMedia.find((item) => item.key === key);
    if (removing) {
      URL.revokeObjectURL(removing.previewUrl);
      previewUrlsRef.current.delete(removing.previewUrl);
    }

    const remainingLocalMedia = localMedia.filter((item) => item.key !== key);
    setLocalMedia(remainingLocalMedia);
    if (mainKey === key) {
      const firstExisting = visibleExistingImages[0];
      setMainKey(firstExisting ? `existing-${firstExisting.id}` : remainingLocalMedia[0]?.key || null);
    }
    setMessage(undefined);
  };

  return (
    <section aria-labelledby="product-media-title" className="rounded-xl border border-border bg-surface-strong p-4 sm:p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="product-media-title" className="text-base font-semibold text-foreground">Medya</h2>
          <p className="mt-1 text-sm leading-5 text-muted">En fazla 10 görsel seçin, önizleyin ve ana görseli belirleyin.</p>
        </div>
        <span className="rounded-md bg-surface-subtle px-2 py-1 text-xs font-bold tabular-nums text-muted">{totalCount}/{MAX_PRODUCT_IMAGES}</span>
      </div>

      <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-5">
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
              onSelectMain={setMainKey}
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
            onSelectMain={setMainKey}
            onRemove={removeLocalMedia}
          />
        ))}

        {totalCount < MAX_PRODUCT_IMAGES ? (
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            className="group aspect-square min-h-28 rounded-xl border-2 border-dashed border-border-strong bg-surface-subtle/45 text-muted transition-colors hover:border-primary hover:bg-primary-soft/40 hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
            aria-label="Ürün görseli ekle"
          >
            <span className="mx-auto flex size-10 items-center justify-center rounded-full border border-border-strong bg-surface-strong text-2xl font-light leading-none transition-colors group-hover:border-primary">+</span>
            <span className="mt-2 block text-xs font-bold">Görsel ekle</span>
          </button>
        ) : null}
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        multiple
        className="sr-only"
        onChange={(event) => addFiles(event.target.files)}
        aria-label="Ürün görsellerini seç"
      />

      {message ? <p className="mt-3 text-sm font-semibold text-warning" role="status">{message}</p> : null}
      {images.length > MAX_PRODUCT_IMAGES ? <p className="mt-3 text-xs text-warning">Mevcut kayıtların ilk 10 görseli gösteriliyor.</p> : null}
      {localMedia.length > 0 ? (
        <p className="mt-3 rounded-lg border border-blue-200 bg-blue-50 px-3 py-2 text-xs leading-5 text-blue-900">
          Seçilen dosyalar yalnızca bu ekranda önizlenir. Bulut yükleme servisi bağlanana kadar kaydetme işleminde backend&apos;e gönderilmez.
        </p>
      ) : null}
    </section>
  );
}

// Burada her görseli ana seçim ve isteğe bağlı yerel kaldırma aksiyonlarıyla aynı kare kartta sunuyorum.
function MediaCard({
  mediaKey,
  src,
  alt,
  label,
  isMain,
  onSelectMain,
  onRemove,
}: {
  mediaKey: string;
  src: string;
  alt: string;
  label: string;
  isMain: boolean;
  onSelectMain: (key: string) => void;
  onRemove?: (key: string) => void;
}) {
  const [failed, setFailed] = useState(false);

  return (
    <article className={`group relative aspect-square min-h-28 overflow-hidden rounded-xl border-2 bg-surface-subtle transition-colors ${isMain ? "border-primary ring-2 ring-primary/15" : "border-border hover:border-border-strong"}`}>
      {failed ? (
        <span className="flex size-full items-center justify-center text-xs font-semibold text-muted">Görsel açılamadı</span>
      ) : (
        <img src={src} alt={alt} onError={() => setFailed(true)} className="size-full object-cover" />
      )}
      <div className="absolute inset-x-0 bottom-0 flex items-end justify-between gap-2 bg-gradient-to-t from-slate-950/80 via-slate-950/35 to-transparent px-2 pb-2 pt-8 text-white">
        <span className="min-w-0 truncate text-[11px] font-semibold">{label}</span>
        <button
          type="button"
          onClick={() => onSelectMain(mediaKey)}
          className={`shrink-0 rounded-md px-2 py-1 text-[11px] font-bold ${isMain ? "bg-primary text-white" : "bg-white/90 text-slate-800 hover:bg-white"}`}
          aria-pressed={isMain}
        >
          {isMain ? "Ana" : "Ana yap"}
        </button>
      </div>
      {onRemove ? (
        <button
          type="button"
          onClick={() => onRemove(mediaKey)}
          aria-label={`${label} görselini kaldır`}
          className="absolute right-2 top-2 flex size-7 items-center justify-center rounded-full bg-slate-950/70 text-sm font-bold text-white opacity-100 transition-opacity hover:bg-danger sm:opacity-0 sm:group-hover:opacity-100 sm:focus-visible:opacity-100"
        >
          ×
        </button>
      ) : null}
    </article>
  );
}
