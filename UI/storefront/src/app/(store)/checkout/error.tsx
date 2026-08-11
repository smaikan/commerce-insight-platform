"use client";

// Burada kargo seçenekleri yüklenemediğinde kullanıcıya aynı route'u güvenli biçimde yeniden deneme imkânı veriyorum.
export default function CheckoutError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <main id="main-content" className="page-shell flex flex-1 items-center justify-center py-16 sm:py-24">
      <section className="w-full max-w-xl rounded-2xl border border-line bg-surface px-6 py-10 text-center shadow-panel sm:px-10">
        <h1 className="text-2xl font-semibold tracking-[-0.03em] text-ink">Sipariş sayfası yüklenemedi</h1>
        <p className="mt-3 text-sm leading-6 text-ink-muted">Kargo seçeneklerine şu anda ulaşılamıyor.</p>
        <button type="button" onClick={reset} className="focus-ring mt-6 min-h-12 rounded-lg bg-brand-700 px-6 text-sm font-bold text-white hover:bg-brand-950">Tekrar dene</button>
      </section>
    </main>
  );
}
