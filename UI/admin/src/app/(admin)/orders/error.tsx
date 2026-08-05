"use client";

// Burada sipariş liste veya detay isteği başarısız olduğunda aynı route üzerinde gerçek yeniden deneme sunuyorum.
export default function OrdersError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <section className="rounded-xl border border-danger/30 bg-surface p-6" role="alert">
      <h1 className="text-xl font-semibold text-foreground">Sipariş bilgileri yüklenemedi</h1>
      <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">API bağlantısını kontrol edip tekrar deneyin. Liste filtreleriniz URL&apos;de korunur.</p>
      <button type="button" onClick={reset} className="mt-4 min-h-10 rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Tekrar dene</button>
    </section>
  );
}
