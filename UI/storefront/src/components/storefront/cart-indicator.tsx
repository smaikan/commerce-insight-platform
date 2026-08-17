"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import {
  clearCartStateForOwnerChange,
  loadCart,
  subscribeToCart,
} from "@/modules/cart/client/cart-api";
import { useHeaderSession } from "@/modules/auth/components/header-session";

// Burada header'ı hafif tutup sepet sayısını ortak client snapshot'ından, ek mutation isteği üretmeden gösteriyorum.
export function CartIndicator() {
  const session = useHeaderSession();
  // Burada header'ın sunucu ve tarayıcı ilk render'ını eşitleyip sayacı hydration sonrasında ortak snapshot'tan dolduruyorum.
  const [quantityState, setQuantityState] = useState<{
    owner: "guest" | "authenticated";
    quantity: number;
  } | null>(null);

  useEffect(() => {
    if (session === "loading") return;

    // Burada çözülmüş guest/authenticated owner değişiminde eski sepet belleğini taşımadan otoriter sepeti yeniden okuyorum.
    clearCartStateForOwnerChange();
    const owner = session;
    let active = true;
    const unsubscribe = subscribeToCart((cart) => setQuantityState({ owner, quantity: cart.totalQuantity }));

    function readCart() {
      // Burada login merge veya oturum değişiminden kalabilecek eski client snapshot'ı yerine güncel CartDto'yu zorla okuyorum.
      void loadCart(true)
        .then((cart) => {
          if (active) setQuantityState({ owner, quantity: cart.totalQuantity });
        })
        .catch(() => {
          if (active) setQuantityState(null);
        });
    }

    // Burada header sayacını kritik render ve ilk görsel istekleriyle yarıştırmadan tarayıcının boş zamanında yüklüyorum.
    const idleId = window.requestIdleCallback?.(readCart, { timeout: 1_500 });
    const timeoutId = idleId === undefined ? window.setTimeout(readCart, 300) : undefined;

    return () => {
      active = false;
      unsubscribe();
      if (idleId !== undefined) window.cancelIdleCallback(idleId);
      if (timeoutId !== undefined) window.clearTimeout(timeoutId);
    };
  }, [session]);

  // Burada yeni owner'ın sepeti yüklenene kadar önceki kullanıcıya ait adedi göstermiyorum.
  const quantity = quantityState?.owner === session ? quantityState.quantity : null;
  const accessibleQuantity = quantity ?? 0;

  return (
    <Link
      href="/cart"
      prefetch={false}
      className="header-action relative inline-flex size-9 items-center justify-center p-0! sm:size-11 sm:px-3! sm:py-2.5!"
      aria-label={`Sepet, ${accessibleQuantity} ürün`}
    >
      <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 sm:size-6" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <path d="M3.5 5h2l1.5 9.2a2 2 0 0 0 2 1.7h7.6a2 2 0 0 0 1.9-1.4L20.5 8H6" />
        <circle cx="9.5" cy="19" r="1" />
        <circle cx="17" cy="19" r="1" />
      </svg>
      {quantity !== null && quantity > 0 ? (
        <span className="absolute top-0.5 right-0.5 inline-flex min-h-5 min-w-5 items-center justify-center rounded-full bg-brand-700 px-1 text-[0.625rem] font-bold leading-none text-white" aria-hidden="true">
          {quantity > 99 ? "99+" : quantity}
        </span>
      ) : null}
    </Link>
  );
}
