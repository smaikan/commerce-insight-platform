"use client";

import { useState } from "react";

import {
  cartErrorMessage,
  isConflictProblem,
  loadCart,
  mutateCart,
} from "../../cart/client/cart-api";

export type PurchaseVariant = {
  id: string;
  name: string;
  value: string;
  price: number;
  stock: number;
};

type SubmissionState =
  | { kind: "idle" }
  | { kind: "pending" }
  | { kind: "success"; message: string }
  | { kind: "error"; message: string };

// Burada varyantlı üründe seçimi, varyantsız üründe doğrudan guest cart mutation'ını en küçük Client Component sınırında tutuyorum.
export function ProductPurchasePanel({
  variants,
  currency,
  showVariantSelection,
}: {
  variants: PurchaseVariant[];
  currency: string;
  showVariantSelection: boolean;
}) {
  const firstAvailable = variants.find((variant) => variant.stock > 0);
  const [selectedId, setSelectedId] = useState(firstAvailable?.id || variants[0]?.id || "");
  const [submission, setSubmission] = useState<SubmissionState>({ kind: "idle" });
  const selectedVariant = variants.find((variant) => variant.id === selectedId);
  const canAdd = Boolean(selectedVariant && selectedVariant.stock > 0 && submission.kind !== "pending");

  async function addToCart() {
    if (!selectedVariant || selectedVariant.stock <= 0 || submission.kind === "pending") return;

    setSubmission({ kind: "pending" });

    try {
      // Burada mutation öncesinde sepeti sunucudan taze okuyup başka sekme veya önceki işlemden kalan token'ı kullanmıyorum.
      const currentCart = await loadCart(true);
      const addedCart = await mutateCart("/api/cart/items", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          productVariantId: selectedVariant.id,
          quantity: 1,
          ...(currentCart.concurrencyToken
            ? { expectedConcurrencyToken: currentCart.concurrencyToken }
            : {}),
        }),
      });

      setSubmission({
        kind: "success",
        message: `Sepete eklendi. Sepetinizde ${addedCart.totalQuantity} ürün var.`,
      });
    } catch (error) {
      if (isConflictProblem(error)) {
        await loadCart(true).catch(() => undefined);
      }
      setSubmission({ kind: "error", message: cartErrorMessage(error) });
    }
  }

  if (variants.length === 0) {
    return (
      <section className="mt-6" aria-label="Satın alma">
        <p className="text-sm text-danger">Bu ürün için satın alınabilir bir seçenek bulunmuyor.</p>
      </section>
    );
  }

  return (
    <section className="mt-6" aria-labelledby={showVariantSelection ? "variant-heading" : undefined} aria-label={showVariantSelection ? undefined : "Satın alma"}>
      {showVariantSelection ? (
        <>
          <div className="flex items-center justify-between gap-4">
            <h2 id="variant-heading" className="text-sm font-bold text-ink">Ürün seçeneği</h2>
            <span className="text-xs text-ink-muted">{variants.length} seçenek</span>
          </div>

          <fieldset aria-labelledby="variant-heading">
            <legend className="sr-only">Ürün seçeneği</legend>
            <div className="mt-3 grid gap-2">
              {variants.map((variant) => {
                const isSelected = variant.id === selectedId;
                const isUnavailable = variant.stock <= 0;

                return (
                  <label
                    key={variant.id}
                    className={`flex min-h-16 items-start gap-3 rounded-xl border bg-surface p-4 transition-colors ${
                      isSelected ? "border-brand-700" : "border-line"
                    } ${isUnavailable ? "text-ink-muted" : "cursor-pointer"}`}
                  >
                    <input
                      type="radio"
                      name="productVariant"
                      value={variant.id}
                      checked={isSelected}
                      disabled={isUnavailable}
                      onChange={() => {
                        setSelectedId(variant.id);
                        setSubmission({ kind: "idle" });
                      }}
                      className="mt-0.5 size-4 shrink-0 accent-brand-700"
                    />
                    <span className="min-w-0 flex-1">
                      <span className="block text-sm font-semibold text-ink">
                        {[variant.name, variant.value].filter(Boolean).join(" · ")}
                      </span>
                      <span className={`mt-1 block text-xs ${isUnavailable ? "text-danger" : "text-ink-muted"}`}>
                        {isUnavailable ? "Tükendi" : "Mevcut"}
                      </span>
                    </span>
                    <span className="shrink-0 text-sm font-bold text-ink">{formatVariantPrice(variant.price, currency)}</span>
                  </label>
                );
              })}
            </div>
          </fieldset>
        </>
      ) : null}

      <button
        type="button"
        disabled={!canAdd}
        aria-busy={submission.kind === "pending"}
        onClick={addToCart}
        className={`focus-ring min-h-12 w-full rounded-lg bg-brand-700 px-5 py-3 text-sm font-bold text-white transition-colors hover:bg-brand-950 disabled:cursor-not-allowed disabled:bg-line disabled:text-ink-muted ${showVariantSelection ? "mt-4" : ""}`}
      >
        {submission.kind === "pending" ? "Sepete ekleniyor…" : selectedVariant?.stock ? "Sepete ekle" : "Stokta yok"}
      </button>

      {submission.kind === "success" ? (
        <p className="mt-3 text-sm font-semibold text-success" role="status">{submission.message}</p>
      ) : null}
      {submission.kind === "error" ? (
        <p className="mt-3 text-sm font-semibold text-danger" role="alert">{submission.message}</p>
      ) : null}
    </section>
  );
}

function formatVariantPrice(value: number, currency: string): string {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
  }).format(value);
}
