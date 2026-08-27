"use client";

import { useRef, useState } from "react";

import {
  initializeIyzicoCheckoutForm,
  paymentIntentKey,
  paymentProblemMessage,
  redirectToPaymentPage,
} from "@/modules/checkout/client/checkout-api";
import { SandboxPaymentNotice } from "@/modules/checkout/components/sandbox-payment-notice";

// Burada retry sırasında aynı ödeme intent anahtarını koruyup yeni deneme yalnız açıkça istendiğinde yeni anahtar üretiyorum.
export function IyzicoPaymentControl({ orderId, newAttempt = false, sandboxCardNumber = null }: { orderId: string; newAttempt?: boolean; sandboxCardNumber?: string | null }) {
  const submittingRef = useRef(false);
  const intentKeyRef = useRef<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string>();

  async function startPayment() {
    if (submittingRef.current) return;
    submittingRef.current = true;
    setIsSubmitting(true);
    setError(undefined);

    try {
      intentKeyRef.current ||= paymentIntentKey(orderId, newAttempt);
      const session = await initializeIyzicoCheckoutForm(orderId, intentKeyRef.current);
      redirectToPaymentPage(session);
    } catch (cause) {
      setError(paymentProblemMessage(cause));
    } finally {
      submittingRef.current = false;
      setIsSubmitting(false);
    }
  }

  return (
    <div className="space-y-3">
      {sandboxCardNumber ? <SandboxPaymentNotice cardNumber={sandboxCardNumber} /> : null}
      {error ? <p className="mb-3 rounded-lg border border-danger/25 bg-danger/5 px-3 py-3 text-sm leading-5 text-danger" role="alert">{error}</p> : null}
      <button
        type="button"
        onClick={() => void startPayment()}
        disabled={isSubmitting}
        aria-busy={isSubmitting}
        className="focus-ring inline-flex min-h-12 w-full cursor-pointer items-center justify-center rounded-lg bg-brand-700 px-5 text-sm font-bold text-white transition-colors hover:bg-brand-950 disabled:cursor-wait disabled:bg-line disabled:text-ink-muted"
      >
        {isSubmitting ? "Güvenli ödeme hazırlanıyor…" : newAttempt ? "Ödemeyi yeniden dene" : "Ödemeye devam et"}
      </button>
    </div>
  );
}
