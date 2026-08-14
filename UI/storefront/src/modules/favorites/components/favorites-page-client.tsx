"use client";

import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

import {
  favoriteErrorMessage,
  loadFavoriteProducts,
  subscribeToFavoriteSettled,
} from "@/modules/favorites/client/favorites-api";
import { FavoritesView } from "@/modules/favorites/components/favorites-view";
import type { FavoriteProductPage } from "@/modules/favorites/types";

// Burada guest ve authenticated favori sayfasını ortak BFF'den yükleyip tamamlanan mutasyonlarla uzlaştırıyorum.
export function FavoritesPageClient({ pageNumber, pageSize }: { pageNumber: number; pageSize: number }) {
  const router = useRouter();
  const [products, setProducts] = useState<FavoriteProductPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const loadPage = useCallback(async (showLoading = true) => {
    if (showLoading) {
      setLoading(true);
      setError(null);
    }
    try {
      const nextProducts = await loadFavoriteProducts(pageNumber, pageSize);
      if (nextProducts.totalPages > 0 && pageNumber > nextProducts.totalPages) {
        router.replace(`/account/favorites?page=${nextProducts.totalPages}&pageSize=${pageSize}`);
        return;
      }
      setProducts(nextProducts);
    } catch (loadError) {
      setError(favoriteErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  }, [pageNumber, pageSize, router]);

  useEffect(() => {
    let active = true;
    void loadFavoriteProducts(pageNumber, pageSize)
      .then((nextProducts) => {
        if (!active) return;
        if (nextProducts.totalPages > 0 && pageNumber > nextProducts.totalPages) {
          router.replace(`/account/favorites?page=${nextProducts.totalPages}&pageSize=${pageSize}`);
          return;
        }
        setProducts(nextProducts);
        setLoading(false);
      })
      .catch((loadError: unknown) => {
        if (!active) return;
        setError(favoriteErrorMessage(loadError));
        setLoading(false);
      });

    const unsubscribe = subscribeToFavoriteSettled(() => void loadPage());
    return () => {
      active = false;
      unsubscribe();
    };
  }, [loadPage, pageNumber, pageSize, router]);

  if (loading && !products) {
    return <p role="status" className="py-16 text-center text-sm text-ink-muted">Favorileriniz yükleniyor…</p>;
  }

  if (error && !products) {
    return (
      <section className="border border-line bg-surface px-6 py-12 text-center" aria-labelledby="favorites-error-title">
        <h1 id="favorites-error-title" className="text-2xl font-black text-brand-950">Favoriler yüklenemedi</h1>
        <p className="mx-auto mt-3 max-w-md text-sm leading-6 text-ink-muted">{error}</p>
        <button type="button" onClick={() => void loadPage()} className="focus-ring mt-6 min-h-11 bg-brand-950 px-5 text-sm font-bold text-white hover:bg-brand-700">Tekrar dene</button>
      </section>
    );
  }

  return products ? <FavoritesView products={products} /> : null;
}
