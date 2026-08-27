"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import {
  confirmationProblemMessage,
  loadCheckoutOrder,
} from "@/modules/checkout/client/checkout-api";
import { IyzicoPaymentControl } from "@/modules/checkout/components/iyzico-payment-control";
import { authoritativePaymentState, type AuthoritativePaymentState } from "@/modules/checkout/payment-state";
import type { CheckoutOrder } from "@/modules/checkout/types";

const POLL_INTERVAL_MS = 2_000;
const MAX_POLL_COUNT = 6;

type ResultState =
  | { kind: "loading" }
  | { kind: "ready"; order: CheckoutOrder; paymentState: AuthoritativePaymentState; exhausted: boolean }
  | { kind: "error"; message: string };

// Burada ödeme dönüş parametrelerini yalnız yön bulma ipucu sayıp gerçek sonucu owner-scoped sipariş GET'iyle sınırlı süre boyunca doğruluyorum.
export function PaymentResult({ orderId, sandboxCardNumber }: { orderId: string; sandboxCardNumber: string | null }) {
  const [state, setState] = useState<ResultState>({ kind: "loading" });
  const [refreshVersion, setRefreshVersion] = useState(0);

  const refresh = useCallback(() => {
    setState({ kind: "loading" });
    setRefreshVersion((version) => version + 1);
  }, []);

  useEffect(() => {
    let active = true;
    let timer: ReturnType<typeof setTimeout> | undefined;

    async function poll(attempt: number) {
      try {
        const order = await loadCheckoutOrder(orderId);
        if (!active) return;
        const paymentState = authoritativePaymentState(order);
        const exhausted = paymentState === "pending" && attempt >= MAX_POLL_COUNT;
        setState({ kind: "ready", order, paymentState, exhausted });
        if (paymentState === "pending" && !exhausted) {
          timer = setTimeout(() => void poll(attempt + 1), POLL_INTERVAL_MS);
        }
      } catch (error) {
        if (active) setState({ kind: "error", message: confirmationProblemMessage(error) });
      }
    }

    void poll(0);
    return () => {
      active = false;
      if (timer) clearTimeout(timer);
    };
  }, [orderId, refreshVersion]);

  if (state.kind === "loading") return <PaymentResultShell title="Ödeme durumu doğrulanıyor" message="Bankadan gelen sonuç sipariş kaydınızla karşılaştırılıyor…" busy />;
  if (state.kind === "error") return <PaymentResultShell title="Ödeme durumu açılamadı" message={state.message} orderId={orderId} onRefresh={refresh} />;

  if (state.paymentState === "paid") {
    return <PaymentResultShell title="Ödemeniz alındı" message="Siparişinizin ödemesi doğrulandı ve sipariş kaydınız güncellendi." orderId={orderId} tone="success" />;
  }

  if (state.paymentState === "failed") {
    return (
      <PaymentResultShell title="Ödeme tamamlanamadı" message="Kart bilgileri mağazaya aktarılmadı. Siparişiniz için güvenli ödeme sayfasında yeni bir deneme başlatabilirsiniz." orderId={orderId} tone="danger">
        <IyzicoPaymentControl orderId={orderId} newAttempt sandboxCardNumber={sandboxCardNumber} />
      </PaymentResultShell>
    );
  }

  return (
    <PaymentResultShell
      title="Ödeme sonucu bekleniyor"
      message={state.exhausted ? "Sonuç henüz kesinleşmedi. Sipariş durumunu yeniden kontrol edebilirsiniz." : "Ödeme sağlayıcısından kesin sonuç bekleniyor; bu sayfa kısa süre içinde otomatik güncellenecek."}
      orderId={orderId}
      busy={!state.exhausted}
      onRefresh={state.exhausted ? refresh : undefined}
    />
  );
}

function PaymentResultShell({ title, message, orderId, tone = "neutral", busy = false, onRefresh, children }: { title: string; message: string; orderId?: string; tone?: "neutral" | "success" | "danger"; busy?: boolean; onRefresh?: () => void; children?: React.ReactNode }) {
  const icon = tone === "success" ? "✓" : tone === "danger" ? "!" : "…";
  return (
    <main id="main-content" className="page-shell flex flex-1 items-center justify-center py-16 sm:py-24">
      <section className="w-full max-w-xl rounded-2xl border border-line bg-surface px-6 py-10 text-center shadow-panel sm:px-10" aria-live="polite" aria-busy={busy}>
        <span className={`mx-auto inline-flex size-12 items-center justify-center rounded-full text-xl font-black ${tone === "danger" ? "bg-danger/10 text-danger" : "bg-surface-subtle text-brand-700"}`} aria-hidden="true">{icon}</span>
        <h1 className="mt-5 text-2xl font-semibold tracking-[-0.03em] text-ink sm:text-3xl">{title}</h1>
        <p className="mt-3 text-sm leading-6 text-ink-muted">{message}</p>
        {children ? <div className="mt-6">{children}</div> : null}
        <div className="mt-6 flex flex-col justify-center gap-3 sm:flex-row">
          {onRefresh ? <button type="button" onClick={onRefresh} className="focus-ring inline-flex min-h-11 items-center justify-center rounded-lg bg-brand-700 px-5 text-sm font-bold text-white hover:bg-brand-950">Durumu yenile</button> : null}
          {orderId ? <Link href={`/checkout/confirmation/${encodeURIComponent(orderId)}`} className="focus-ring inline-flex min-h-11 items-center justify-center rounded-lg border border-brand-700 px-5 text-sm font-bold text-brand-700 hover:bg-surface-subtle">Sipariş detayını aç</Link> : null}
        </div>
      </section>
    </main>
  );
}
