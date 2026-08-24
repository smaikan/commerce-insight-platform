"use client";

/* eslint-disable @next/next/no-img-element */

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { ConfirmDialog } from "@/lib/admin/components/confirm-dialog";
import { deleteProductImageAction } from "@/modules/products/actions";
import { validateProductImageFile } from "@/modules/products/cloudinary-upload";
import {
  MAX_PRODUCT_IMAGES,
  moveMediaKey,
  type ProductMediaDraft,
  type ProductMediaDraftItem,
} from "@/modules/products/product-media";
import type { ProductImage } from "@/modules/products/types";

type LocalMedia = ProductMediaDraftItem & {
  previewUrl: string;
};

type ProductMediaEditorProps = {
  productId?: string;
  images: ProductImage[];
  disabled?: boolean;
  onDirty: () => void;
  onDraftChange: (draft: ProductMediaDraft) => void;
};

// Burada kayıtlı görsellerle yeni dosyaları aynı kontrollü medya alanında yönetiyorum.
export function ProductMediaEditor({ productId, images, disabled = false, onDirty, onDraftChange }: ProductMediaEditorProps) {
  const router = useRouter();
  const visibleExistingImages = images.slice(0, MAX_PRODUCT_IMAGES);
  const initialMainImage = visibleExistingImages.find((image) => image.isMain) || visibleExistingImages[0];
  const [localMedia, setLocalMedia] = useState<LocalMedia[]>([]);
  const [mainKey, setMainKey] = useState<string | null>(initialMainImage ? `existing-${initialMainImage.id}` : null);
  const [orderedKeys, setOrderedKeys] = useState<string[]>(() => visibleExistingImages.map((image) => `existing-${image.id}`));
  const [draggedKey, setDraggedKey] = useState<string>();
  const [message, setMessage] = useState<string>();
  const [deleteCandidate, setDeleteCandidate] = useState<ProductImage>();
  const [deleting, setDeleting] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const previewUrlsRef = useRef(new Set<string>());
  const totalCount = visibleExistingImages.length + localMedia.length;

  // Burada onaylanan kayıtlı görseli silip backend'in ana görsel seçimini yeniden okuyorum.
  const deleteExistingImage = async () => {
    if (!productId || !deleteCandidate || deleting) return;
    setDeleting(true);
    const result = await deleteProductImageAction(productId, deleteCandidate.id);
    setDeleting(false);
    if (result.status === "error") {
      setMessage(result.message);
      return;
    }
    setDeleteCandidate(undefined);
    setMessage(result.message);
    router.refresh();
  };

  // Burada üst formun yükleme sırasında kullanacağı dosya ve ana görsel seçimini güncel tutuyorum.
  useEffect(() => {
    onDraftChange({
      localMedia: localMedia.map(({ key, file }) => ({ key, file })),
      mainKey,
      orderedKeys,
    });
  }, [localMedia, mainKey, onDraftChange, orderedKeys]);

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
      setOrderedKeys((current) => [...current, ...nextMedia.map((item) => item.key)]);
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
    const remainingKeys = orderedKeys.filter((item) => item !== key);
    setLocalMedia(remainingLocalMedia);
    setOrderedKeys(remainingKeys);
    if (mainKey === key) {
      setMainKey(remainingKeys[0] || null);
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

  // Burada sürükleme ve klavye taşıma işlemlerini aynı deterministik anahtar sırasına uygularım.
  const moveMedia = (sourceKey: string, targetKey: string) => {
    if (disabled || sourceKey === targetKey) return;
    setOrderedKeys((current) => moveMediaKey(current, sourceKey, targetKey));
    onDirty();
  };

  const moveMediaByOffset = (key: string, offset: -1 | 1) => {
    const index = orderedKeys.indexOf(key);
    const targetKey = orderedKeys[index + offset];
    if (targetKey) moveMedia(key, targetKey);
  };

  const existingByKey = new Map(visibleExistingImages.map((image) => [`existing-${image.id}`, image]));
  const localByKey = new Map(localMedia.map((item) => [item.key, item]));

  return (
    <section aria-labelledby="product-media-title" aria-busy={disabled} className="rounded-xl border border-border bg-surface-strong p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="product-media-title" className="text-base font-semibold text-foreground">Medya</h2>
          <p className="mt-0.5 text-xs leading-5 text-muted">JPG, PNG veya WebP biçiminde en fazla 10 görsel; her dosya en fazla 8 MB.</p>
        </div>
        <span className="rounded-md bg-surface-subtle px-2 py-1 text-xs font-bold tabular-nums text-muted">{totalCount}/{MAX_PRODUCT_IMAGES}</span>
      </div>

      <p className="mt-3 text-xs leading-5 text-muted">Görselleri sürükleyerek veya kartlardaki oklarla sıralayın. Dosya seçicinin verdiği sıra korunur.</p>
      <div className="mt-2 grid grid-cols-2 gap-2 sm:grid-cols-4 xl:grid-cols-6">
        {orderedKeys.map((key, index) => {
          const existing = existingByKey.get(key);
          const local = localByKey.get(key);
          if (!existing && !local) return null;
          return (
            <MediaCard
              key={key}
              mediaKey={key}
              src={existing?.imageUrl || local?.previewUrl || ""}
              alt={existing?.altText || local?.file.name || "Ürün görseli"}
              label={existing?.altText || local?.file.name || "Kayıtlı görsel"}
              position={index + 1}
              total={orderedKeys.length}
              isMain={mainKey === key}
              isDragging={draggedKey === key}
              disabled={disabled}
              onSelectMain={selectMain}
              onMoveBackward={() => moveMediaByOffset(key, -1)}
              onMoveForward={() => moveMediaByOffset(key, 1)}
              onDragStart={() => setDraggedKey(key)}
              onDragOver={(sourceKey) => moveMedia(sourceKey, key)}
              onDragEnd={() => setDraggedKey(undefined)}
              onRemove={existing && productId ? () => setDeleteCandidate(existing) : local ? removeLocalMedia : undefined}
            />
          );
        })}

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
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        title="Ürün görseli silinsin mi?"
        description={deleteCandidate?.isMain ? "Ana görsel silinecek. Sıradaki uygun görsel otomatik olarak ana görsel olacak." : "Seçilen görsel ürün kaydından kalıcı olarak kaldırılacak."}
        confirmLabel="Görseli sil"
        pending={deleting}
        error={message && deleteCandidate ? message : undefined}
        onCancel={() => { if (!deleting) { setDeleteCandidate(undefined); setMessage(undefined); } }}
        onConfirm={deleteExistingImage}
      />
    </section>
  );
}

// Burada her görseli erişilebilir ana seçim ve isteğe bağlı kaldırma aksiyonlarıyla sunuyorum.
function MediaCard({
  mediaKey,
  src,
  alt,
  label,
  position,
  total,
  isMain,
  isDragging,
  disabled,
  onSelectMain,
  onMoveBackward,
  onMoveForward,
  onDragStart,
  onDragOver,
  onDragEnd,
  onRemove,
}: {
  mediaKey: string;
  src: string;
  alt: string;
  label: string;
  position: number;
  total: number;
  isMain: boolean;
  isDragging: boolean;
  disabled: boolean;
  onSelectMain: (key: string) => void;
  onMoveBackward: () => void;
  onMoveForward: () => void;
  onDragStart: () => void;
  onDragOver: (sourceKey: string) => void;
  onDragEnd: () => void;
  onRemove?: (key: string) => void;
}) {
  const [failed, setFailed] = useState(false);

  return (
    <article
      draggable={!disabled}
      onDragStart={(event) => {
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", mediaKey);
        onDragStart();
      }}
      onDragOver={(event) => {
        event.preventDefault();
        event.dataTransfer.dropEffect = "move";
      }}
      onDrop={(event) => {
        event.preventDefault();
        const sourceKey = event.dataTransfer.getData("text/plain");
        if (sourceKey) onDragOver(sourceKey);
        onDragEnd();
      }}
      onDragEnd={onDragEnd}
      aria-label={`${label}, sıra ${position}/${total}${isMain ? ", ana görsel" : ""}`}
      className={`group relative aspect-square min-h-24 overflow-hidden rounded-lg border-2 bg-surface-subtle transition-colors ${disabled ? "cursor-default" : "cursor-grab active:cursor-grabbing"} ${isDragging ? "opacity-60" : ""} ${isMain ? "border-primary ring-2 ring-primary/15" : "border-border hover:border-border-strong"}`}
    >
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
      <div role="group" className="absolute left-2 top-2 flex overflow-hidden rounded-md border border-white/50 bg-black/70 text-white shadow-sm" aria-label={`${label} sıralama kontrolleri`}>
        <button
          type="button"
          disabled={disabled || position === 1}
          onClick={onMoveBackward}
          aria-label={`${label} görselini önceki sıraya taşı`}
          className="flex size-7 items-center justify-center text-sm font-bold hover:bg-black disabled:cursor-not-allowed disabled:opacity-40"
        >
          ←
        </button>
        <span className="flex min-w-7 items-center justify-center border-x border-white/30 px-1 text-[10px] font-bold tabular-nums" aria-hidden="true">{position}</span>
        <button
          type="button"
          disabled={disabled || position === total}
          onClick={onMoveForward}
          aria-label={`${label} görselini sonraki sıraya taşı`}
          className="flex size-7 items-center justify-center text-sm font-bold hover:bg-black disabled:cursor-not-allowed disabled:opacity-40"
        >
          →
        </button>
      </div>
    </article>
  );
}
