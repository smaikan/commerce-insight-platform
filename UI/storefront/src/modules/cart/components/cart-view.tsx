"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import {
  cartErrorMessage,
  isConflictProblem,
  loadCart,
  mutateCart,
  subscribeToCart,
} from "@/modules/cart/client/cart-api";
import type { Cart } from "@/modules/cart/types";
import { formatVariantLabel } from "@/lib/formatting/variant";

type LoadState =
  | { kind: "loading" }
  | { kind: "ready"; cart: Cart }
  | { kind: "error"; message: string };

// Burada sepetin veri yükleme ve mutation durumlarını tek, dar Client Component sınırında yönetiyorum.
export function CartView({ currency }: { currency: string }) {
  // Burada sunucu HTML'i ile tarayıcının ilk render'ını aynı loading durumunda başlatıp hydration farkını önlüyorum.
  const [state, setState] = useState<LoadState>({ kind: "loading" });
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const [notice, setNotice] = useState<{ kind: "error" | "info"; message: string } | null>(null);
  const [confirmClear, setConfirmClear] = useState(false);

  useEffect(() => {
    const unsubscribe = subscribeToCart((cart) => setState({ kind: "ready", cart }));
    void loadCart()
      .then((cart) => setState({ kind: "ready", cart }))
      .catch((error) => setState({ kind: "error", message: cartErrorMessage(error) }));
    return unsubscribe;
  }, []);

  async function retryLoad() {
    setState({ kind: "loading" });
    setNotice(null);
    try {
      const cart = await loadCart(true);
      setState({ kind: "ready", cart });
    } catch (error) {
      setState({ kind: "error", message: cartErrorMessage(error) });
    }
  }

  async function runMutation(action: string, operation: (cart: Cart, token: string) => Promise<Cart>) {
    if (state.kind !== "ready" || pendingAction) return;

    setPendingAction(action);
    setNotice(null);
    setConfirmClear(false);

    try {
      // Burada her mutation öncesinde güncel sepet token'ını sunucudan alarak sekmeler arası eski snapshot kullanımını önlüyorum.
      const currentCart = await loadCart(true);
      if (!currentCart.concurrencyToken) throw new Error("missing_cart_token");
      const cart = await operation(currentCart, currentCart.concurrencyToken);
      setState({ kind: "ready", cart });
    } catch (error) {
      if (isConflictProblem(error)) {
        try {
          const cart = await loadCart(true);
          setState({ kind: "ready", cart });
        } catch {
          // Burada asıl conflict mesajını koruyup yenileme hatasıyla kullanıcıya ikinci, belirsiz hata göstermiyorum.
        }
      }
      setNotice({ kind: "error", message: cartErrorMessage(error) });
    } finally {
      setPendingAction(null);
    }
  }

  if (state.kind === "loading") return <CartLoadingState />;

  if (state.kind === "error") {
    return (
      <main id="main-content" className="page-shell flex flex-1 items-center justify-center py-16 sm:py-24">
        <section className="w-full max-w-xl rounded-2xl border border-line bg-surface px-6 py-10 text-center shadow-panel sm:px-10" aria-labelledby="cart-error-title">
          <h1 id="cart-error-title" className="text-2xl font-semibold tracking-[-0.03em] text-ink">Sepet yüklenemedi</h1>
          <p className="mt-3 text-sm leading-6 text-ink-muted">{state.message}</p>
          <button type="button" onClick={retryLoad} className="focus-ring mt-6 min-h-11 rounded-lg bg-brand-700 px-5 text-sm font-bold text-white hover:bg-brand-950">
            Tekrar dene
          </button>
        </section>
      </main>
    );
  }

  const cart = state.cart;
  if (cart.items.length === 0) return <EmptyCart />;

  const isMutating = pendingAction !== null;

  return (
    <main id="main-content" className="page-shell max-w-[80rem] flex-1 py-8 sm:py-12 lg:py-14">
      <header className="flex flex-wrap items-end justify-between gap-4 border-b border-line pb-6 sm:pb-8">
        <div>
          <p className="mb-2 text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">Alışveriş</p>
          <h1 className="text-3xl font-semibold tracking-[-0.04em] text-ink sm:text-4xl">Sepetiniz</h1>
          <p className="mt-3 text-sm text-ink-muted">{cart.totalQuantity} ürün seçtiniz.</p>
        </div>
        {!confirmClear ? (
          <button type="button" disabled={isMutating} onClick={() => setConfirmClear(true)} className="focus-ring min-h-11 px-2 text-sm font-semibold text-ink-muted hover:text-danger disabled:cursor-not-allowed disabled:opacity-50">
            Sepeti temizle
          </button>
        ) : (
          <div className="flex items-center gap-2 rounded-lg border border-line bg-surface px-2 py-1.5" role="group" aria-label="Sepeti temizleme onayı">
            <span className="pl-1 text-xs font-semibold text-ink">Tümünü kaldır?</span>
            <button
              type="button"
              disabled={isMutating}
              onClick={() => void runMutation("clear", (_cart, token) => mutateCart("/api/cart", { method: "DELETE", body: JSON.stringify({ expectedConcurrencyToken: token }) }))}
              className="focus-ring min-h-9 rounded-md bg-danger px-3 text-xs font-bold text-white disabled:opacity-50"
            >
              Evet
            </button>
            <button type="button" onClick={() => setConfirmClear(false)} className="focus-ring min-h-9 px-2 text-xs font-bold text-ink-muted">Vazgeç</button>
          </div>
        )}
      </header>

      {notice ? (
        <p className={`mt-5 rounded-lg border px-4 py-3 text-sm font-semibold ${notice.kind === "error" ? "border-danger/30 bg-danger/5 text-danger" : "border-line bg-surface-subtle text-ink"}`} role={notice.kind === "error" ? "alert" : "status"}>
          {notice.message}
        </p>
      ) : null}

      <div className="mt-7 grid items-start gap-8 lg:grid-cols-[minmax(0,1fr)_minmax(18rem,23rem)] lg:gap-10">
        <section className="overflow-hidden rounded-2xl border border-line bg-surface" aria-label="Sepetteki ürünler" aria-busy={isMutating}>
          {cart.items.map((item, index) => {
            const actionPrefix = `item:${item.id}`;
            const isThisPending = pendingAction?.startsWith(actionPrefix) ?? false;
            const canIncrease = item.isAvailable && item.quantity < item.availableStock;
            return (
              <article key={item.id} className={`p-5 sm:p-6 ${index > 0 ? "border-t border-line" : ""}`}>
                <div className="flex items-start justify-between gap-4">
                  <CartItemIdentity item={item} />
                  <p className="shrink-0 text-right text-sm font-bold text-ink sm:text-base">{formatMoney(item.totalPrice, currency)}</p>
                </div>

                {!item.isAvailable ? (
                  <p className="mt-4 rounded-lg bg-danger/5 px-3 py-2 text-sm font-semibold text-danger" role="status">
                    Bu ürün seçilen adetle şu anda kullanılamıyor. Sepetten kaldırabilir veya daha sonra tekrar deneyebilirsiniz.
                  </p>
                ) : null}

                {item.priceChanged ? (
                  <div className="mt-4 rounded-lg border border-brand-600/25 bg-surface-subtle px-3 py-3 text-sm">
                    <p className="font-semibold text-ink">Birim fiyat değişti</p>
                    <p className="mt-1 text-ink-muted">
                      <span className="line-through">{formatMoney(item.unitPrice, currency)}</span>
                      <span className="ml-2 font-bold text-brand-950">{formatMoney(item.currentUnitPrice, currency)}</span>
                    </p>
                    <button
                      type="button"
                      disabled={isMutating || !item.isAvailable}
                      onClick={() => void runMutation(`${actionPrefix}:price`, (_cart, token) => updateItem(item.id, item.quantity, token))}
                      className="focus-ring mt-2 min-h-10 text-sm font-bold text-brand-700 hover:text-brand-950 disabled:cursor-not-allowed disabled:text-ink-muted"
                    >
                      Güncel fiyatı kabul et
                    </button>
                  </div>
                ) : (
                  <p className="mt-3 text-sm text-ink-muted">Birim fiyat: {formatMoney(item.currentUnitPrice, currency)}</p>
                )}

                <div className="mt-5 flex flex-wrap items-center justify-between gap-3">
                  <div className="inline-flex items-center rounded-lg border border-line bg-surface" aria-label={`${item.productTitle || "Ürün"} adedi`}>
                    <button
                      type="button"
                      disabled={isMutating || !item.isAvailable || item.quantity <= 1}
                      onClick={() => void runMutation(`${actionPrefix}:decrease`, (_cart, token) => updateItem(item.id, item.quantity - 1, token))}
                      className="focus-ring inline-flex size-11 items-center justify-center text-xl text-ink disabled:cursor-not-allowed disabled:text-line"
                      aria-label="Adedi azalt"
                    >
                      −
                    </button>
                    <span className="min-w-10 text-center text-sm font-bold tabular-nums text-ink" aria-live="polite">{item.quantity}</span>
                    <button
                      type="button"
                      disabled={isMutating || !canIncrease}
                      onClick={() => void runMutation(`${actionPrefix}:increase`, (_cart, token) => updateItem(item.id, item.quantity + 1, token))}
                      className="focus-ring inline-flex size-11 items-center justify-center text-xl text-ink disabled:cursor-not-allowed disabled:text-line"
                      aria-label="Adedi artır"
                    >
                      +
                    </button>
                  </div>
                  <button
                    type="button"
                    disabled={isMutating}
                    onClick={() => void runMutation(`${actionPrefix}:remove`, (_cart, token) => removeItem(item.id, token))}
                    className="focus-ring min-h-11 px-2 text-sm font-semibold text-ink-muted hover:text-danger disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    {isThisPending ? "İşleniyor…" : "Kaldır"}
                  </button>
                </div>
              </article>
            );
          })}
        </section>

        <aside className="rounded-2xl border border-line bg-surface p-5 shadow-panel sm:p-6 lg:sticky lg:top-28" aria-labelledby="cart-summary-title">
          <h2 id="cart-summary-title" className="text-lg font-bold text-ink">Sipariş özeti</h2>
          <dl className="mt-5 space-y-4 text-sm">
            <div className="flex items-center justify-between gap-4 text-ink-muted">
              <dt>Ürün adedi</dt>
              <dd className="font-semibold tabular-nums text-ink">{cart.totalQuantity}</dd>
            </div>
            <div className="flex items-center justify-between gap-4 border-t border-line pt-4">
              <dt className="font-semibold text-ink">Ara toplam</dt>
              <dd className="text-lg font-bold tabular-nums text-brand-950">{formatMoney(cart.subTotal, currency)}</dd>
            </div>
          </dl>

          {cart.hasUnavailableItems ? (
            <p className="mt-5 rounded-lg bg-danger/5 px-3 py-3 text-sm leading-5 text-danger">Devam etmeden önce kullanılamayan ürünleri kaldırın.</p>
          ) : null}
          {cart.hasPriceChanges ? (
            <p className="mt-3 rounded-lg bg-surface-subtle px-3 py-3 text-sm leading-5 text-ink">Ara toplamın güncellenmesi için değişen fiyatları kabul edin.</p>
          ) : null}

          {!cart.hasUnavailableItems && !cart.hasPriceChanges ? (
            <Link href="/checkout" className="focus-ring mt-6 inline-flex min-h-12 w-full items-center justify-center rounded-lg bg-brand-700 px-4 text-sm font-bold text-white hover:bg-brand-950">
              Teslimata geç
            </Link>
          ) : (
            <button type="button" disabled className="mt-6 min-h-12 w-full cursor-not-allowed rounded-lg bg-line px-4 text-sm font-bold text-ink-muted">Teslimata geç</button>
          )}
          <p className="mt-3 text-xs leading-5 text-ink-muted">Fiyat ve stok bilgileri sipariş oluşturulurken sunucudan yeniden doğrulanır.</p>
          <Link href="/products" className="focus-ring mt-4 inline-flex min-h-11 w-full items-center justify-center rounded-lg border border-brand-700 px-4 text-sm font-bold text-brand-700 hover:bg-surface-subtle">
            Alışverişe devam et
          </Link>
        </aside>
      </div>
    </main>
  );
}

function EmptyCart() {
  return (
    <main id="main-content" className="page-shell flex flex-1 items-center justify-center py-16 sm:py-24">
      <section className="w-full max-w-xl rounded-2xl border border-line bg-surface px-6 py-12 text-center shadow-panel sm:px-10" aria-labelledby="empty-cart-title">
        <span className="mx-auto inline-flex size-14 items-center justify-center rounded-full bg-surface-subtle text-brand-700" aria-hidden="true">
          <svg viewBox="0 0 24 24" className="size-7" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
            <path d="M3.5 5h2l1.5 9.2a2 2 0 0 0 2 1.7h7.6a2 2 0 0 0 1.9-1.4L20.5 8H6" />
            <circle cx="9.5" cy="19" r="1" /><circle cx="17" cy="19" r="1" />
          </svg>
        </span>
        <h1 id="empty-cart-title" className="mt-5 text-2xl font-semibold tracking-[-0.03em] text-ink sm:text-3xl">Sepetiniz henüz boş</h1>
        <p className="mx-auto mt-3 max-w-sm text-sm leading-6 text-ink-muted">Beğendiğiniz ürünleri sepete eklediğinizde burada görebilir, adetlerini kolayca düzenleyebilirsiniz.</p>
        <Link href="/products" className="focus-ring mt-7 inline-flex min-h-12 items-center justify-center rounded-lg bg-brand-700 px-6 text-sm font-bold text-white hover:bg-brand-950">
          Ürünleri keşfet
        </Link>
      </section>
    </main>
  );
}

export function CartLoadingState() {
  return (
    <main id="main-content" className="page-shell max-w-[80rem] flex-1 py-8 sm:py-12 lg:py-14" aria-label="Sepet yükleniyor" aria-busy="true">
      <div className="h-4 w-20 animate-pulse rounded bg-line" />
      <div className="mt-3 h-10 w-52 animate-pulse rounded bg-line" />
      <div className="mt-10 grid gap-8 lg:grid-cols-[minmax(0,1fr)_minmax(18rem,23rem)]">
        <div className="h-64 animate-pulse rounded-2xl border border-line bg-surface" />
        <div className="h-64 animate-pulse rounded-2xl border border-line bg-surface" />
      </div>
    </main>
  );
}

function updateItem(itemId: string, quantity: number, token: string): Promise<Cart> {
  return mutateCart(`/api/cart/items/${encodeURIComponent(itemId)}`, {
    method: "PUT",
    body: JSON.stringify({ quantity, expectedConcurrencyToken: token }),
  });
}

function removeItem(itemId: string, token: string): Promise<Cart> {
  return mutateCart(`/api/cart/items/${encodeURIComponent(itemId)}`, {
    method: "DELETE",
    body: JSON.stringify({ expectedConcurrencyToken: token }),
  });
}

// Burada sepet satırında ürün kimliğini ve yalnızca eksiksiz API varyant seçimini teknik SKU fallback'i olmadan sunuyorum.
export function CartItemIdentity({ item }: { item: Cart["items"][number] }) {
  const variantLabel = formatVariantLabel(item.variantName, item.variantValue);

  return (
    <div className="min-w-0">
      <h2 className="text-base font-bold leading-6 text-ink sm:text-lg">{item.productTitle || "Ürün"}</h2>
      {variantLabel ? <p className="mt-1 text-xs text-ink-muted">{variantLabel}</p> : null}
    </div>
  );
}

function formatMoney(value: number, currency: string): string {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
  }).format(value);
}
