"use client";

import { useEffect, useRef, useState } from "react";
import { validateImageFile } from "@/lib/cloudinary/browser-upload";
import { replaceStoreSettingsMedia } from "@/modules/settings/store-settings/media-client";
import type { StoreSettingsMediaSlot } from "@/modules/settings/store-settings/media-slots";

type StoreImageFieldProps = {
  id: string;
  label: string;
  hint: string;
  slot: StoreSettingsMediaSlot;
  value: string | null;
  disabled?: boolean;
  error?: string;
  onChange: (value: string | null) => void;
};

export function StoreImageField({ id, label, hint, slot, value, disabled, error, onChange }: StoreImageFieldProps) {
  const controllerRef = useRef<AbortController | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string>();
  const [failedPreviewUrl, setFailedPreviewUrl] = useState<string>();

  useEffect(() => () => controllerRef.current?.abort(), []);

  const upload = async (file: File | undefined) => {
    if (!file) return;
    const validationError = validateImageFile(file);
    if (validationError) {
      setUploadError(validationError);
      return;
    }

    controllerRef.current?.abort();
    const controller = new AbortController();
    controllerRef.current = controller;
    setUploading(true);
    setUploadError(undefined);
    try {
      // Burada her StoreSettings alanını server-side imzalı sabit Cloudinary yuvasında değiştiriyorum.
      const asset = await replaceStoreSettingsMedia(slot, file, controller.signal);
      onChange(asset.secureUrl);
    } catch (caught) {
      if (!controller.signal.aborted) setUploadError(caught instanceof Error ? caught.message : "Görsel yüklenemedi.");
    } finally {
      if (!controller.signal.aborted) setUploading(false);
    }
  };

  const message = error || uploadError;
  return (
    <div className="rounded-xl border border-border bg-surface-subtle/35 p-3">
      <div className="flex items-start gap-3">
        <div className="flex h-16 w-24 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-border bg-surface">
          {value && failedPreviewUrl !== value ? (
            // Store settings may contain an existing non-Cloudinary URL, so the native element keeps the preview origin-agnostic.
            // eslint-disable-next-line @next/next/no-img-element
            <img src={value} alt="" className="h-full w-full object-contain p-1" onError={() => setFailedPreviewUrl(value)} />
          ) : (
            <span className="text-xs font-semibold text-muted" aria-hidden="true">Görsel yok</span>
          )}
        </div>
        <div className="min-w-0 flex-1">
          <label htmlFor={id} className="text-sm font-semibold text-foreground">{label}</label>
          <p className="mt-0.5 text-xs leading-5 text-muted">{hint}</p>
          <div className="mt-2 flex flex-wrap gap-2">
            <label className={`inline-flex min-h-9 items-center justify-center rounded-lg border border-border-strong bg-surface px-3 text-xs font-semibold text-foreground hover:bg-surface-subtle ${disabled || uploading ? "pointer-events-none opacity-60" : "cursor-pointer"}`}>
              {uploading ? "Görsel yükleniyor…" : value ? "Değiştir" : "Görsel yükle"}
              <input
                id={id}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                disabled={disabled || uploading}
                className="sr-only"
                onChange={(event) => { void upload(event.target.files?.[0]); event.currentTarget.value = ""; }}
              />
            </label>
            {value ? (
              <button type="button" disabled={disabled || uploading} onClick={() => onChange(null)} className="min-h-9 rounded-lg px-3 text-xs font-semibold text-danger hover:bg-danger/10 disabled:opacity-60">
                Kaldır
              </button>
            ) : null}
          </div>
        </div>
      </div>
      {message ? <p role="alert" className="mt-2 text-xs font-medium text-danger">{message}</p> : null}
    </div>
  );
}
