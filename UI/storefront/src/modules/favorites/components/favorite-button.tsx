"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

import { useHeaderSession } from "@/modules/auth/components/header-session";
import {
  isFavoriteAuthenticationError,
  isFavoriteProduct,
  loadFavoriteState,
  mutateFavorite,
  subscribeToFavorites,
} from "@/modules/favorites/client/favorites-api";
import { FavoriteHeartIcon } from "@/modules/favorites/components/favorite-icon";

type FavoriteButtonProps = {
  productId: string;
  productTitle: string;
  variant?: "card" | "detail";
  refreshOnChange?: boolean;
  initiallyFavorite?: boolean;
};

let favoriteRefreshStarted = false;

// Burada guest ve authenticated oturumların favori mutasyonu yapabildiğini, yalnız session yüklenirken beklenmesi gerektiğini belirliyorum.
export function canToggleFavorite(session: ReturnType<typeof useHeaderSession>): boolean {
  return session !== "loading";
}

// Burada mevcut sayfayı giriş veya refresh akışına güvenli göreli dönüş hedefi olarak hazırlıyorum.
function currentReturnTo(): string {
  return `${window.location.pathname}${window.location.search}`;
}

// Burada cookie yazan refresh Route Handler'a tam sayfa geçişi yaparak süresi dolmuş oturumu güvenilir biçimde yeniliyorum.
function refreshFavoriteSession(): void {
  if (favoriteRefreshStarted) return;
  favoriteRefreshStarted = true;
  window.location.assign(`/api/auth/refresh?returnTo=${encodeURIComponent(currentReturnTo())}`);
}

// Burada ürün kalbini guest ve authenticated kullanıcılar için aynı owner-scoped favori kaynağına bağlıyorum.
export function FavoriteButton({
  productId,
  productTitle,
  variant = "card",
  refreshOnChange = false,
  initiallyFavorite = false,
}: FavoriteButtonProps) {
  const router = useRouter();
  const session = useHeaderSession();
  const [isFavorite, setIsFavorite] = useState(() => initiallyFavorite || isFavoriteProduct(productId));
  const [isPending, setIsPending] = useState(false);

  useEffect(() => {
    let active = true;
    void loadFavoriteState()
      .then((state) => {
        if (active) setIsFavorite(state.productIds.includes(productId));
      })
      .catch((error: unknown) => {
        if (!active) return;
        if (session === "authenticated" && isFavoriteAuthenticationError(error)) {
          refreshFavoriteSession();
          return;
        }
      })
      .finally(() => undefined);

    const unsubscribe = subscribeToFavorites((state) => {
      if (active) setIsFavorite(state.productIds.includes(productId));
    });
    return () => {
      active = false;
      unsubscribe();
    };
  }, [productId, session]);

  // Burada favori mutation'ını çift tıklamaya kapatıp 404 ve conflict durumlarını authoritative listeyle uzlaştırıyorum.
  async function handleFavoriteToggle() {
    if (!canToggleFavorite(session) || isPending) return;

    const nextFavorite = !isFavorite;
    setIsPending(true);
    try {
      await mutateFavorite(productId, nextFavorite);
      if (refreshOnChange) router.refresh();
    } catch (error) {
      if (session === "authenticated" && isFavoriteAuthenticationError(error)) {
        refreshFavoriteSession();
        return;
      }

      if (error && typeof error === "object" && [404, 409].includes(Number((error as { status?: unknown }).status))) {
        await loadFavoriteState(true).catch(() => undefined);
      }
    } finally {
      setIsPending(false);
    }
  }

  const displayedFavorite = initiallyFavorite ? isFavorite : session !== "loading" && isFavorite;
  const actionLabel = displayedFavorite ? `${productTitle} ürününü favorilerden çıkar` : `${productTitle} ürününü favorilere ekle`;
  const busy = session === "loading" || isPending;

  return (
    <div className={variant === "card" ? "absolute top-1.5 right-1.5 z-10 sm:top-2 sm:right-2" : "shrink-0"}>
      <button
        type="button"
        aria-label={actionLabel}
        aria-pressed={displayedFavorite}
        aria-busy={busy}
        disabled={session === "loading" || isPending}
        onClick={handleFavoriteToggle}
        className={variant === "card"
          ? `focus-ring group/favorite inline-flex size-11 cursor-pointer items-center justify-center rounded-full transition-transform duration-200 hover:scale-105 disabled:cursor-wait disabled:opacity-60 ${displayedFavorite ? "text-brand-700" : "text-ink/75 hover:text-brand-700"}`
          : `focus-ring inline-flex size-11 cursor-pointer items-center justify-center rounded-full transition-colors duration-200 disabled:cursor-wait disabled:opacity-60 ${displayedFavorite ? "bg-brand-50 text-brand-700" : "text-ink-muted hover:bg-surface-subtle hover:text-brand-700"}`}
      >
        {variant === "card" ? (
          <span className={`flex size-8 items-center justify-center rounded-full ring-1 shadow-[0_2px_10px_rgb(15_23_42/0.08)] backdrop-blur-md transition-colors ${displayedFavorite ? "bg-white/95 ring-brand-700/15" : "bg-white/82 ring-black/5 group-hover/favorite:bg-white/95"}`}>
            <FavoriteHeartIcon filled={displayedFavorite} className="size-[1.125rem]" strokeWidth={1.55} />
          </span>
        ) : (
          <FavoriteHeartIcon filled={displayedFavorite} className="size-[1.3rem]" strokeWidth={1.5} />
        )}
      </button>
    </div>
  );
}
