"use client";

/* eslint-disable @next/next/no-img-element */

import { useState } from "react";

// Burada koleksiyon listesindeki görseli sabit geometride gösterip kırık veya eksik URL'yi yer tutucuya döndürüyorum.
export function CollectionThumbnail({ src, name }: { src?: string | null; name: string }) {
  const [failed, setFailed] = useState(false);
  return (
    <span className="flex h-11 w-14 shrink-0 items-center justify-center overflow-hidden rounded-md border border-border bg-surface-subtle">
      {src && !failed ? (
        <img src={src} alt="" loading="lazy" onError={() => setFailed(true)} className="size-full object-cover" />
      ) : (
        <svg viewBox="0 0 24 24" className="size-5 fill-none stroke-muted stroke-[1.5]" aria-label={`${name} için görsel yok`} role="img">
          <path d="M4 5.5h16v13H4z" /><path d="m4 15 4-4 3 3 2-2 7 6.5" /><circle cx="15.5" cy="9" r="1.5" />
        </svg>
      )}
    </span>
  );
}
