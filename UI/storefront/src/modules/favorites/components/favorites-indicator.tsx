"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import {
  getFavoriteSnapshot,
  loadFavoriteState,
  subscribeToFavorites,
} from "@/modules/favorites/client/favorites-api";
import { FavoriteHeartIcon } from "@/modules/favorites/components/favorite-icon";

// Burada navbar kalbini guest ve authenticated owner'ın gerçek favori sayısıyla sunuyorum.
export function FavoritesIndicator() {
  const [count, setCount] = useState(() => getFavoriteSnapshot().totalCount);

  useEffect(() => {
    let active = true;
    void loadFavoriteState(true)
      .then((state) => {
        if (active) setCount(state.totalCount);
      })
      .catch(() => {
        if (active) setCount(0);
      });
    const unsubscribe = subscribeToFavorites((state) => {
      if (active) setCount(state.totalCount);
    });
    return () => {
      active = false;
      unsubscribe();
    };
  }, []);

  const href = "/account/favorites";
  const label = count > 0 ? `Favorilerim, ${count} ürün` : "Favorilerim";

  return (
    <Link
      href={href}
      prefetch={false}
      aria-label={label}
      className="header-action relative inline-flex size-9 items-center justify-center p-0! text-ink hover:bg-surface-subtle hover:text-brand-700 sm:size-11 sm:px-3! sm:py-2.5!"
    >
      <FavoriteHeartIcon filled={count > 0} className="size-4.5 sm:size-5" />
      {count > 0 ? (
        <span className="absolute top-1 right-0.5 flex min-w-4.5 items-center justify-center rounded-full bg-brand-700 px-1 text-[0.625rem] leading-4 font-bold text-white" aria-hidden="true">
          {count > 99 ? "99+" : count}
        </span>
      ) : null}
    </Link>
  );
}
