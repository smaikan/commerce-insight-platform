"use client";

import type { ReactNode } from "react";
import { Fragment, useState } from "react";
import Link from "next/link";
import { formatOrderAmount } from "@/modules/orders/presentation";
import type { OrderListPreview, OrderPreviewError } from "@/modules/orders/types";

type PreviewState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; data: OrderListPreview }
  | { status: "error"; message: string; traceId?: string };

// Burada yalnız açma etkileşimini ve istek durumunu istemciye taşıyıp tablonun statik hücrelerini Server Component olarak koruyorum.
export function OrderExpandableRows({
  orderId,
  orderNumber,
  orderHref,
  children,
}: {
  orderId: string;
  orderNumber: string;
  orderHref: string;
  children: ReactNode;
}) {
  const [isExpanded, setIsExpanded] = useState(false);
  const [preview, setPreview] = useState<PreviewState>({ status: "idle" });
  const previewId = `order-preview-${orderId}`;

  // Burada satırı anında açıp özeti yalnız ilk ihtiyaçta yüklüyor, başarılı sonucu yeniden açılışlar için yerel durumda tutuyorum.
  function togglePreview() {
    const nextExpanded = !isExpanded;
    setIsExpanded(nextExpanded);
    if (nextExpanded && preview.status === "idle") void loadPreview();
  }

  // Burada same-origin BFF cevabını no-store isteyip başarısız durumda satır içinde güvenli yeniden deneme sunuyorum.
  async function loadPreview() {
    setPreview({ status: "loading" });
    try {
      const response = await fetch(`/api/orders/${encodeURIComponent(orderId)}/preview`, {
        cache: "no-store",
        credentials: "same-origin",
        headers: { Accept: "application/json" },
      });
      const payload: unknown = await response.json().catch(() => null);

      if (!response.ok) {
        const problem = isOrderPreviewError(payload) ? payload : undefined;
        setPreview({
          status: "error",
          message: problem?.message ?? "Sipariş özeti yüklenemedi. Lütfen tekrar deneyin.",
          traceId: problem?.traceId,
        });
        return;
      }

      if (!isOrderListPreview(payload)) {
        setPreview({ status: "error", message: "Sipariş özeti beklenmeyen bir biçimde geldi." });
        return;
      }

      setPreview({ status: "ready", data: payload });
    } catch {
      setPreview({ status: "error", message: "Sipariş özeti yüklenemedi. Bağlantınızı kontrol edip tekrar deneyin." });
    }
  }

  return (
    <Fragment>
      <tr className={`group align-middle transition-colors ${isExpanded ? "bg-primary-soft/35" : "bg-surface-strong hover:bg-primary-soft/25"}`}>
        {children}
        <td className="px-4 py-3 text-right">
          <button
            type="button"
            aria-expanded={isExpanded}
            aria-controls={previewId}
            onClick={togglePreview}
            className={`inline-flex min-h-10 items-center justify-center gap-2 rounded-lg border px-3 text-xs font-semibold outline-none transition-colors focus-visible:ring-2 focus-visible:ring-focus focus-visible:ring-offset-2 ${isExpanded ? "border-primary/30 bg-surface-strong text-primary" : "border-border bg-surface-strong text-foreground hover:border-primary/30 hover:text-primary"}`}
          >
            <span>{isExpanded ? "Kapat" : "Hızlı bakış"}</span>
            <svg aria-hidden="true" viewBox="0 0 20 20" className={`size-4 fill-none stroke-current stroke-2 transition-transform ${isExpanded ? "rotate-180" : ""}`}>
              <path d="m5 7.5 5 5 5-5" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </button>
        </td>
      </tr>

      {isExpanded ? (
        <tr className="bg-surface-subtle/55">
          <td colSpan={6} className="px-4 pb-4 pt-0 sm:px-5">
            <section id={previewId} aria-label={`${orderNumber} hızlı sipariş özeti`} className="overflow-hidden rounded-xl border border-primary/15 bg-surface-strong shadow-sm">
              {preview.status === "loading" || preview.status === "idle" ? <PreviewLoading /> : null}
              {preview.status === "error" ? <PreviewFailure preview={preview} onRetry={loadPreview} /> : null}
              {preview.status === "ready" ? <PreviewContent preview={preview.data} orderHref={orderHref} /> : null}
            </section>
          </td>
        </tr>
      ) : null}
    </Fragment>
  );
}

// Burada yükleme durumunu son içerik düzenine yakın iskeletlerle ve ekran okuyucu durum mesajıyla gösteriyorum.
function PreviewLoading() {
  return (
    <div className="grid gap-5 p-5 lg:grid-cols-[minmax(0,1.35fr)_minmax(17rem,0.65fr)]" role="status" aria-live="polite">
      <span className="sr-only">Müşteri ve sipariş bilgileri yükleniyor.</span>
      <div>
        <div className="h-5 w-44 animate-pulse rounded bg-surface-subtle" />
        <div className="mt-3 h-4 w-64 animate-pulse rounded bg-surface-subtle" />
        <div className="mt-6 space-y-3">
          <div className="h-14 animate-pulse rounded-lg bg-surface-subtle" />
          <div className="h-14 animate-pulse rounded-lg bg-surface-subtle" />
        </div>
      </div>
      <div className="h-36 animate-pulse rounded-lg bg-surface-subtle" />
    </div>
  );
}

// Burada yükleme hatasını ilgili satır içinde tutup kullanıcının bağlamını kaybetmeden tekrar denemesini sağlıyorum.
function PreviewFailure({ preview, onRetry }: { preview: Extract<PreviewState, { status: "error" }>; onRetry: () => void }) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-4 p-5" role="alert">
      <div>
        <p className="font-semibold text-foreground">Hızlı görünüm açılamadı</p>
        <p className="mt-1 text-sm leading-6 text-muted">{preview.message}</p>
        {preview.traceId ? <p className="mt-1 font-mono text-xs text-muted">Takip kodu: {preview.traceId}</p> : null}
      </div>
      <button type="button" onClick={() => void onRetry()} className="min-h-10 rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:border-primary/35 hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">
        Tekrar dene
      </button>
    </div>
  );
}

// Burada müşteri, alınan ürünler ve teslimat adresini hızlı operasyon taramasına uygun iki sütunlu düzende bir araya getiriyorum.
function PreviewContent({ preview, orderHref }: { preview: OrderListPreview; orderHref: string }) {
  const customerName = preview.customer
    ? `${preview.customer.firstName} ${preview.customer.lastName}`.trim()
    : "Müşteri bilgisi bulunmuyor";

  return (
    <div>
      <header className="flex flex-wrap items-start justify-between gap-4 border-b border-border bg-primary-soft/20 px-5 py-4">
        <div className="min-w-0">
          <p className="text-xs font-semibold uppercase tracking-[0.08em] text-muted">Müşteri</p>
          <h3 className="mt-1 text-base font-bold text-foreground">{customerName}</h3>
          {preview.customer ? (
            <p className="mt-1 flex flex-wrap gap-x-3 gap-y-1 text-sm text-muted">
              <span className="break-all">{preview.customer.email}</span>
              <span>{preview.customer.phoneNumber}</span>
            </p>
          ) : null}
        </div>
        <Link href={orderHref} className="inline-flex min-h-10 items-center justify-center gap-2 rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus focus-visible:ring-offset-2">
          Siparişi görüntüle
          <svg aria-hidden="true" viewBox="0 0 20 20" className="size-4 fill-none stroke-current stroke-2">
            <path d="m7 4 6 6-6 6" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </Link>
      </header>

      <div className="grid gap-0 lg:grid-cols-[minmax(0,1.35fr)_minmax(17rem,0.65fr)]">
        <div className="min-w-0 p-5 lg:border-r lg:border-border">
          <div className="flex items-center justify-between gap-4">
            <h4 className="text-sm font-bold text-foreground">Alınan ürünler</h4>
            <span className="text-xs font-semibold text-muted">{preview.items.length} kalem</span>
          </div>
          {preview.items.length > 0 ? (
            <ul className="mt-3 divide-y divide-border rounded-lg border border-border">
              {preview.items.map((item) => (
                <li key={item.id} className="grid gap-2 px-3 py-3 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center">
                  <div className="min-w-0">
                    <Link href={`/products/${encodeURIComponent(item.productId)}`} className="font-semibold text-foreground hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">
                      {item.productTitle}
                    </Link>
                    <p className="mt-1 truncate font-mono text-xs text-muted">SKU: {item.variantSku}</p>
                  </div>
                  <div className="flex items-center justify-between gap-5 text-sm sm:justify-end">
                    <span className="text-muted">{item.quantity} adet</span>
                    <span className="min-w-24 text-right font-bold tabular-nums text-foreground">{formatOrderAmount(item.totalPrice)}</span>
                  </div>
                </li>
              ))}
            </ul>
          ) : <p className="mt-3 text-sm text-muted">Bu siparişte ürün kalemi bulunmuyor.</p>}
          <div className="mt-4 flex items-end justify-between gap-4 border-t border-border pt-4">
            <span className="text-sm font-semibold text-muted">Sipariş toplamı</span>
            <span className="text-lg font-bold tabular-nums text-foreground">{formatOrderAmount(preview.grandTotal)}</span>
          </div>
        </div>

        <div className="p-5">
          <h4 className="text-sm font-bold text-foreground">Teslimat adresi</h4>
          {preview.shippingAddress ? (
            <address className="mt-3 not-italic text-sm leading-6 text-muted">
              <p className="font-semibold text-foreground">{preview.shippingAddress.firstName} {preview.shippingAddress.lastName}</p>
              <p>{preview.shippingAddress.title}</p>
              <p className="mt-2 text-foreground/80">{preview.shippingAddress.fullAddress}</p>
              <p>{preview.shippingAddress.district} / {preview.shippingAddress.city}{preview.shippingAddress.postalCode ? ` · ${preview.shippingAddress.postalCode}` : ""}</p>
              <p className="mt-3 font-semibold text-foreground">{preview.shippingAddress.phoneNumber}</p>
            </address>
          ) : <p className="mt-3 text-sm leading-6 text-muted">Teslimat adresi bulunmuyor.</p>}
        </div>
      </div>
    </div>
  );
}

// Burada istemciye gelen başarılı cevabın en azından render için gereken temel yapıyı taşıdığını doğruluyorum.
function isOrderListPreview(value: unknown): value is OrderListPreview {
  if (!value || typeof value !== "object") return false;
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.id === "string" &&
    typeof candidate.orderNumber === "string" &&
    typeof candidate.grandTotal === "number" &&
    Array.isArray(candidate.items) &&
    candidate.items.every(isPreviewItem)
  );
}

// Burada her ürün özetinin liste görünümünde kullanılan alanlarını çalışma zamanında kontrol ediyorum.
function isPreviewItem(value: unknown): boolean {
  if (!value || typeof value !== "object") return false;
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.id === "string" &&
    typeof candidate.productId === "string" &&
    typeof candidate.productTitle === "string" &&
    typeof candidate.variantSku === "string" &&
    typeof candidate.quantity === "number" &&
    typeof candidate.totalPrice === "number"
  );
}

// Burada hata cevabında yalnız güvenli metin ve opsiyonel takip kodu alanlarını kabul ediyorum.
function isOrderPreviewError(value: unknown): value is OrderPreviewError {
  if (!value || typeof value !== "object") return false;
  const candidate = value as Record<string, unknown>;
  return typeof candidate.message === "string" && (candidate.traceId === undefined || typeof candidate.traceId === "string");
}
