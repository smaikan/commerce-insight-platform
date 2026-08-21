"use client";

import Script from "next/script";
import { useCallback, useEffect, useRef } from "react";

type TurnstileOptions = {
  sitekey: string;
  theme: "light";
  size: "compact";
  action: "contact_form";
  callback: (token: string) => void;
  "expired-callback": () => void;
  "error-callback": () => void;
};

type TurnstileApi = {
  render: (container: HTMLElement, options: TurnstileOptions) => string;
  reset: (widgetId: string) => void;
  remove: (widgetId: string) => void;
};

function getTurnstile(): TurnstileApi | undefined {
  return (window as Window & { turnstile?: TurnstileApi }).turnstile;
}

type ContactTurnstileProps = {
  siteKey: string;
  resetVersion: number;
  error?: string;
  onToken: (token: string) => void;
  onExpired: () => void;
  onError: () => void;
};

// Burada production contact challenge'ını contact_form action değeriyle client yaprağında ve fail-closed durumda çalıştırıyorum.
export function ContactTurnstile({
  siteKey,
  resetVersion,
  error,
  onToken,
  onExpired,
  onError,
}: ContactTurnstileProps) {
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

  const renderChallenge = useCallback(() => {
    const api = getTurnstile();
    if (!siteKey || !containerRef.current || !api || widgetIdRef.current) return;
    widgetIdRef.current = api.render(containerRef.current, {
      sitekey: siteKey,
      theme: "light",
      size: "compact",
      action: "contact_form",
      callback: (token) => onTokenRef.current(token),
      "expired-callback": () => onExpiredRef.current(),
      "error-callback": () => onErrorRef.current(),
    });
  }, [siteKey]);

  useEffect(() => {
    const api = getTurnstile();
    if (widgetIdRef.current && api) api.reset(widgetIdRef.current);
  }, [resetVersion]);

  useEffect(() => () => {
    const api = getTurnstile();
    if (widgetIdRef.current && api) {
      api.remove(widgetIdRef.current);
      widgetIdRef.current = null;
    }
  }, []);

  return (
    <section className="rounded-xl border border-brand-700/30 bg-surface-subtle p-4" aria-labelledby="contact-challenge-title">
      <h3 id="contact-challenge-title" className="text-sm font-bold text-ink">Güvenlik doğrulaması</h3>
      <p className="mt-1 text-xs leading-5 text-ink-muted">Mesajınızı korumak için kısa doğrulamayı tamamlayın.</p>
      {siteKey ? (
        <>
          <Script
            id="contact-turnstile"
            src="https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit"
            strategy="afterInteractive"
            onReady={renderChallenge}
            onError={onError}
          />
          <div ref={containerRef} className="mt-4 flex min-h-[8.75rem] justify-center" />
        </>
      ) : (
        <p className="mt-3 text-sm font-semibold text-danger" role="alert">
          Güvenlik doğrulaması şu anda başlatılamıyor. Lütfen daha sonra tekrar deneyin.
        </p>
      )}
      {error ? <p className="mt-3 text-sm font-semibold text-danger" role="alert">{error}</p> : null}
    </section>
  );
}
