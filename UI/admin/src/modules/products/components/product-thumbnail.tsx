"use client";

/* eslint-disable @next/next/no-img-element */

import { useState } from "react";

// Burada uzak görsel alan adlarını image optimizer'a açmadan kırık URL'leri güvenli ürün yer tutucusuna düşürüyorum.
export function ProductThumbnail({ src, alt }: { src?: string | null; alt: string }) {
  const [failed, setFailed] = useState(false);

  if (!src || failed) {
    return (
      <span className="flex size-12 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-border bg-gradient-to-br from-primary-soft to-surface-subtle text-primary" aria-hidden="true">
        <svg viewBox="0 0 24 24" className="size-5 fill-none stroke-current stroke-[1.7]">
          <path d="m5 8 7-4 7 4-7 4-7-4Z" strokeLinejoin="round" />
          <path d="M5 8v8l7 4 7-4V8M12 12v8" strokeLinejoin="round" />
        </svg>
      </span>
    );
  }

  return (
    <span className="size-12 shrink-0 overflow-hidden rounded-lg border border-border bg-surface-subtle">
      <img src={src} alt={alt} loading="lazy" onError={() => setFailed(true)} className="size-full object-cover" />
    </span>
  );
}
