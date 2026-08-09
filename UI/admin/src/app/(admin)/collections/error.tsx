"use client";

import Link from "next/link";

// Burada koleksiyon verisi yüklenemediğinde güvenli yeniden deneme ve listeye dönüş seçenekleri sunuyorum.
export default function CollectionsError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <section className="rounded-xl border border-danger/30 bg-surface p-5" role="alert" aria-labelledby="collections-error-title">
        <h1 id="collections-error-title" className="text-lg font-semibold text-foreground">Koleksiyonlar yüklenemedi</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">API bağlantısı veya oturum doğrulaması tamamlanamadı. Mevcut verileriniz değiştirilmedi.</p>
        <div className="mt-4 flex flex-wrap gap-2">
          <button type="button" onClick={reset} className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary">Tekrar dene</button>
          <Link href="/settings" className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3.5 text-sm font-semibold text-foreground hover:bg-surface-subtle">Ayarlara dön</Link>
        </div>
      </section>
    </div>
  );
}
