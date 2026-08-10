"use client";

// Burada oturum veya sayfa sınırı çöktüğünde güvenli yeniden deneme yolunu sunuyorum.
export default function BannersError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <section className="rounded-xl border border-danger/30 bg-surface p-5" role="alert" aria-labelledby="banners-error-title">
        <h1 id="banners-error-title" className="text-lg font-semibold text-foreground">Bannerlar yüklenemedi</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">Yönetim ekranı açılamadı. Kaydedilmemiş bir işlem gönderilmedi; tekrar deneyebilirsiniz.</p>
        <button type="button" onClick={reset} className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">
          Tekrar dene
        </button>
      </section>
    </div>
  );
}
