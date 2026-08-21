"use client";

import { useEffect, useRef } from "react";

// Burada ürün detay sayfası açıldığında arka planda tıklama/görüntülenme sayısını bir kez artırıyorum.
export function ProductViewTracker({ productId }: { productId: string }) {
  const trackedRef = useRef<string | null>(null);

  useEffect(() => {
    if (!productId || trackedRef.current === productId) return;
    trackedRef.current = productId;

    void fetch(`/api/products/${encodeURIComponent(productId)}/view`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      keepalive: true,
    }).catch(() => undefined);
  }, [productId]);

  return null;
}
