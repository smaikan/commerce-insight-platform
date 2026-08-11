"use client";

import Script from "next/script";
import { useCallback, useEffect, useRef } from "react";

type TurnstileOptions = {
  sitekey: string;
  theme: "light";
  size: "compact";
  action: string;
  callback: (token: string) => void;
  "expired-callback": () => void;
  "error-callback": () => void;
};

type TurnstileApi = {
  render: (container: HTMLElement, options: TurnstileOptions) => string;
  reset: (widgetId: string) => void;
  remove: (widgetId: string) => void;
};

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}

type TurnstileChallengeProps = {
  siteKey: string;
  resetVersion: number;
  error?: string;
  onToken: (token: string) => void;
  onExpired: () => void;
  onError: () => void;
};

// Burada Turnstile script'ini yalnız API challenge istediğinde yükleyip checkout'ın normal açılış maliyetinden uzak tutuyorum.
export function TurnstileChallenge({
  siteKey,
  resetVersion,
  error,
  onToken,
  onExpired,
  onError,
}: TurnstileChallengeProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const widgetIdRef = useRef<string | null>(null);
  const onTokenRef = useRef(onToken);
  const onExpiredRef = useRef(onExpired);
  const onErrorRef = useRef(onError);

  useEffect(() => {
    onTokenRef.current = onToken;
    onExpiredRef.current = onExpired;
    onErrorRef.current = onError;
  }, [onError, onExpired, onToken]);

  // Burada dinamik checkout durumunda widget'ı açık render yöntemiyle tek kez oluşturup tokenı yalnız parent form state'ine aktarıyorum.
  const renderChallenge = useCallback(() => {
    if (!siteKey || !containerRef.current || !window.turnstile || widgetIdRef.current) return;

    widgetIdRef.current = window.turnstile.render(containerRef.current, {
      sitekey: siteKey,
      theme: "light",
      size: "compact",
      action: "guest_checkout",
      callback: (token) => onTokenRef.current(token),
      "expired-callback": () => onExpiredRef.current(),
      "error-callback": () => onErrorRef.current(),
    });
  }, [siteKey]);

  useEffect(() => {
    if (widgetIdRef.current && window.turnstile) {
      window.turnstile.reset(widgetIdRef.current);
    }
  }, [resetVersion]);

  useEffect(() => () => {
    if (widgetIdRef.current && window.turnstile) {
      window.turnstile.remove(widgetIdRef.current);
      widgetIdRef.current = null;
    }
  }, []);

  return (
    <section className="mt-5 rounded-xl border border-brand-700/30 bg-surface-subtle p-4" aria-labelledby="checkout-challenge-title">
      <h3 id="checkout-challenge-title" className="text-sm font-bold text-ink">Güvenlik doğrulaması</h3>
      <p className="mt-1 text-xs leading-5 text-ink-muted">Siparişinizi korumak için kısa doğrulamayı tamamlayın.</p>
      {siteKey ? (
        <>
          <Script
            id="storefront-turnstile"
            src="https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit"
            strategy="afterInteractive"
            onReady={renderChallenge}
            onError={onError}
          />
          <div ref={containerRef} className="mt-4 flex min-h-[8.75rem] justify-center" />
        </>
      ) : (
        <p className="mt-3 text-sm font-semibold text-danger" role="alert">Güvenlik doğrulaması şu anda başlatılamıyor. Lütfen daha sonra tekrar deneyin.</p>
      )}
      {error ? <p className="mt-3 text-sm font-semibold text-danger" role="alert">{error}</p> : null}
    </section>
  );
}
