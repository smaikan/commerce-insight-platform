"use client";

/* eslint-disable @next/next/no-img-element */

import { useState } from "react";

// Burada eksik veya açılamayan marka görsellerini tablo geometrisini bozmayan bir baş harf alanıyla karşılıyorum.
export function BrandThumbnail({ imageUrl, name }: { imageUrl?: string | null; name: string }) {
  const [failed, setFailed] = useState(false);
  const initial = name.trim().charAt(0).toLocaleUpperCase("tr-TR") || "M";

  return (
    <span className="flex size-11 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-border bg-surface-subtle text-sm font-bold text-muted">
      {imageUrl && !failed ? (
        <img src={imageUrl} alt="" width={44} height={44} loading="lazy" onError={() => setFailed(true)} className="size-full object-contain p-1" />
      ) : (
        <span aria-hidden="true">{initial}</span>
      )}
    </span>
  );
}
