"use client";

import Link from "next/link";

// Burada marka verisi yüklenemediğinde güvenli yeniden deneme ve liste dönüşü sunuyorum.
export default function BrandsError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <section className="rounded-xl border border-danger/30 bg-surface p-5" role="alert" aria-labelledby="brands-error-title">
        <h1 id="brands-error-title" className="text-lg font-semibold text-foreground">Markalar yüklenemedi</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">API bağlantısı veya oturum doğrulaması tamamlanamadı. Mevcut verileriniz değiştirilmedi.</p>
        <div className="mt-4 flex flex-wrap gap-2">
          <button type="button" onClick={reset} className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary">Tekrar dene</button>
          <Link href="/brands" className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-3.5 text-sm font-semibold text-foreground hover:bg-surface-subtle">Markalara dön</Link>
        </div>
      </section>
    </div>
  );
}
