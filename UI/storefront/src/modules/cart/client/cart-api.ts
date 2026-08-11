"use client";

import type { Cart, ClientProblem } from "@/modules/cart/types";

const CART_UPDATED_EVENT = "storefront:cart-updated";

let cartSnapshot: Cart | null = null;
let cartLoadPromise: Promise<Cart> | null = null;
let nextRequestSequence = 0;
let appliedRequestSequence = 0;

// Burada daha eski başlayan bir GET isteğinin sonradan tamamlanıp yeni mutation sonucunu ezmesini engelliyorum.
function publishCart(cart: Cart, requestSequence: number): Cart {
  if (requestSequence < appliedRequestSequence && cartSnapshot) return cartSnapshot;

  appliedRequestSequence = requestSequence;
  cartSnapshot = cart;
  window.dispatchEvent(new CustomEvent<Cart>(CART_UPDATED_EVENT, { detail: cart }));
  return cart;
}

async function requestCart(path: string, init: RequestInit): Promise<Cart> {
  const requestSequence = ++nextRequestSequence;
  const response = await fetch(path, {
    ...init,
    cache: "no-store",
    credentials: "same-origin",
  });
  const body = await response.json().catch(() => null);

  if (!response.ok) {
    const source = body && typeof body === "object" ? (body as Record<string, unknown>) : {};
    throw {
      status: response.status,
      title: typeof source.title === "string" ? source.title : "Sepet isteği tamamlanamadı",
      detail: typeof source.detail === "string" ? source.detail : undefined,
      code: typeof source.code === "string" ? source.code : undefined,
      traceId: typeof source.traceId === "string" ? source.traceId : undefined,
    } satisfies ClientProblem;
  }

  return publishCart(body as Cart, requestSequence);
}

// Burada header ve sepet sayfasının ilk isteğini tek promise üzerinden paylaşarak aynı GET çağrısını çoğaltmıyorum.
export function loadCart(force = false): Promise<Cart> {
  if (!force && cartSnapshot) return Promise.resolve(cartSnapshot);
  if (!force && cartLoadPromise) return cartLoadPromise;

  const pending = requestCart("/api/cart", { method: "GET" });
  cartLoadPromise = pending;

  void pending.finally(() => {
    if (cartLoadPromise === pending) cartLoadPromise = null;
  }).catch(() => undefined);

  return pending;
}

// Burada tüm mutation sonuçlarını ortak snapshot'a yayınlayıp header sayacını ek istek olmadan güncelliyorum.
export function mutateCart(path: string, init: RequestInit): Promise<Cart> {
  return requestCart(path, {
    ...init,
    headers: { "Content-Type": "application/json", ...init.headers },
  });
}

export function getCartSnapshot(): Cart | null {
  return cartSnapshot;
}

export function subscribeToCart(listener: (cart: Cart) => void): () => void {
  function handleUpdate(event: Event) {
    listener((event as CustomEvent<Cart>).detail);
  }

  window.addEventListener(CART_UPDATED_EVENT, handleUpdate);
  return () => window.removeEventListener(CART_UPDATED_EVENT, handleUpdate);
}

export function cartErrorMessage(error: unknown, fallback = "Sepete şu anda ulaşılamıyor. Lütfen tekrar deneyin."): string {
  if (!error || typeof error !== "object") return fallback;

  const problem = error as Partial<ClientProblem>;
  if (problem.code === "concurrency_conflict") return "Sepetiniz başka bir işlemde güncellendi. Son hali yeniden yüklendi; işlemi tekrar deneyebilirsiniz.";
  if (problem.status === 409) return "Ürünün stok veya satış durumu değişti. Lütfen güncel bilgileri kontrol edip tekrar deneyin.";
  if (problem.status === 404) return "Bu ürün seçeneği artık satın alınabilir değil.";
  if (problem.status === 429) return "Çok fazla istek gönderildi. Lütfen kısa bir süre sonra tekrar deneyin.";
  if (problem.status === 400) return problem.detail || "Sepet bilgileri geçerli değil.";
  return fallback;
}

export function isConflictProblem(error: unknown): boolean {
  return Boolean(error && typeof error === "object" && (error as Partial<ClientProblem>).code === "concurrency_conflict");
}
