"use client";

// Burada ürün liste isteği başarısız olduğunda aynı rota üzerinde gerçek bir yeniden deneme aksiyonu sunuyorum.
export default function ProductsError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <section className="rounded-xl border border-danger/30 bg-surface p-6" role="alert">
      <h1 className="text-xl font-semibold text-foreground">Ürün listesi yüklenemedi</h1>
      <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">API bağlantısını kontrol edip işlemi tekrar deneyin. Filtreleriniz URL’de korunur.</p>
      <button type="button" onClick={reset} className="mt-4 min-h-10 rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">
        Tekrar dene
      </button>
    </section>
  );
}
