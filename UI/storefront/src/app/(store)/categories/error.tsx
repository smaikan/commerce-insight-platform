"use client";

// Burada kategori API hatasını sayfa kabuğunu bozmadan, erişilebilir bir yeniden deneme eylemiyle karşılıyorum.
export default function CategoriesError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <main id="main-content" className="page-shell flex flex-1 items-center py-16">
      <section className="w-full rounded-2xl border border-line bg-surface px-6 py-12 text-center shadow-panel sm:px-10" role="alert">
        <p className="text-xs font-bold tracking-[0.14em] text-danger uppercase">Bağlantı sorunu</p>
        <h1 className="mt-3 text-2xl font-semibold text-ink">Kategoriler şu anda yüklenemedi</h1>
        <p className="mx-auto mt-3 max-w-md text-sm leading-6 text-ink-muted">Bağlantınızı kontrol edip kategorileri yeniden yükleyebilirsiniz.</p>
        <button type="button" onClick={reset} className="focus-ring mt-6 rounded-lg bg-brand-700 px-5 py-3 text-sm font-bold text-white hover:bg-brand-950">
          Tekrar dene
        </button>
      </section>
    </main>
  );
}
