"use client";

/* eslint-disable @next/next/no-img-element */

import { useEffect, useMemo, useRef, useState } from "react";
import { MAX_IMAGE_BYTES, validateImageFile } from "@/lib/cloudinary/browser-upload";

export type BrandImageIntent = "keep" | "replace" | "remove";

type BrandImageFieldProps = {
  existingImageUrl?: string | null;
  disabled: boolean;
  file: File | null;
  intent: BrandImageIntent;
  error?: string;
  onChange: (file: File | null, intent: BrandImageIntent) => void;
};

// Burada tek marka görselinin mevcut, yeni seçilmiş ve kaldırılmış durumlarını aynı kontrollü alanda yönetiyorum.
export function BrandImageField({ existingImageUrl, disabled, file, intent, error, onChange }: BrandImageFieldProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const previewUrl = useMemo(() => file ? URL.createObjectURL(file) : undefined, [file]);
  const [previewFailed, setPreviewFailed] = useState(false);
  const [localError, setLocalError] = useState<string>();
  const visibleImageUrl = previewUrl || (intent === "keep" ? existingImageUrl || undefined : undefined);

  // Burada seçilen yerel görsel için oluşturduğum geçici URL'yi değişimde ve kapanışta serbest bırakıyorum.
  useEffect(() => () => {
    if (previewUrl) URL.revokeObjectURL(previewUrl);
  }, [previewUrl]);

  // Burada seçilen dosyayı yükleme başlamadan ortak tür ve boyut kurallarından geçiriyorum.
  const selectFile = (nextFile: File | undefined) => {
    if (!nextFile || disabled) return;
    const validationError = validateImageFile(nextFile);
    if (validationError) {
      setLocalError(validationError);
      if (inputRef.current) inputRef.current.value = "";
      return;
    }
    setLocalError(undefined);
    setPreviewFailed(false);
    onChange(nextFile, "replace");
    if (inputRef.current) inputRef.current.value = "";
  };

  // Burada yeni seçimi iptal ederken mevcut görsele, mevcut görsel kaldırılırken boş duruma dönüyorum.
  const removeImage = () => {
    setLocalError(undefined);
    setPreviewFailed(false);
    onChange(null, existingImageUrl ? "remove" : "keep");
  };

  return (
    <section className="rounded-xl border border-border bg-surface p-4" aria-labelledby="brand-image-title">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 id="brand-image-title" className="text-sm font-semibold text-foreground">Marka görseli</h2>
          <p className="mt-1 text-xs leading-5 text-muted">JPG, PNG veya WebP; en fazla {MAX_IMAGE_BYTES / 1024 / 1024} MB.</p>
        </div>
        {intent === "remove" ? <span className="text-xs font-semibold text-warning">Kaldırılacak</span> : null}
      </div>

      <div className="mt-3 flex min-h-40 items-center justify-center overflow-hidden rounded-lg border border-dashed border-border-strong bg-surface-subtle/45 p-3">
        {visibleImageUrl && !previewFailed ? (
          <img src={visibleImageUrl} alt="Marka görseli önizlemesi" onError={() => setPreviewFailed(true)} className="max-h-36 max-w-full object-contain" />
        ) : (
          <div className="text-center text-muted">
            <span aria-hidden="true" className="mx-auto flex size-10 items-center justify-center rounded-full border border-border-strong bg-surface text-xl">+</span>
            <p className="mt-2 text-xs font-semibold">Görsel seçilmedi</p>
          </div>
        )}
      </div>

      <div className="mt-3 grid grid-cols-2 gap-2">
        <button type="button" disabled={disabled} onClick={() => inputRef.current?.click()} className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60">
          {visibleImageUrl ? "Değiştir" : "Görsel seç"}
        </button>
        <button type="button" disabled={disabled || (!visibleImageUrl && intent !== "remove")} onClick={removeImage} className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60">
          Kaldır
        </button>
      </div>

      <input ref={inputRef} type="file" accept="image/jpeg,image/png,image/webp" disabled={disabled} className="sr-only" aria-label="Marka görseli seç" onChange={(event) => selectFile(event.target.files?.[0])} />
      {localError || error ? <p role="alert" className="mt-2 text-xs font-semibold text-danger">{localError || error}</p> : null}
    </section>
  );
}
