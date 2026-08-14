import Link from "next/link";

// Burada bulunamayan Storefront rotalarını Türkçe, responsive ve klavyeyle erişilebilir bir geri dönüş ekranına taşıyorum.
export default function NotFound() {
  return (
    <main id="main-content" className="page-shell flex flex-1 items-center justify-center py-16 sm:py-24">
      <section className="w-full max-w-xl rounded-2xl border border-line bg-surface px-6 py-12 text-center shadow-panel sm:px-10" aria-labelledby="not-found-title">
        <p className="text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">404 · Sayfa bulunamadı</p>
        <h1 id="not-found-title" className="mt-3 text-3xl font-semibold tracking-[-0.04em] text-ink sm:text-4xl">
          Aradığınız içerik burada değil
        </h1>
        <p className="mx-auto mt-4 max-w-md text-sm leading-6 text-ink-muted sm:text-base">
          Bağlantı değişmiş, ürün yayından kaldırılmış veya adres hatalı yazılmış olabilir.
        </p>
        <div className="mt-7 flex flex-col justify-center gap-3 sm:flex-row">
          <Link href="/products" className="focus-ring inline-flex min-h-12 items-center justify-center rounded-lg bg-brand-700 px-6 text-sm font-bold text-white hover:bg-brand-950">
            Ürünleri keşfet
          </Link>
          <Link href="/" className="focus-ring inline-flex min-h-12 items-center justify-center rounded-lg border border-brand-700 px-6 text-sm font-bold text-brand-700 hover:bg-surface-subtle">
            Ana sayfaya dön
          </Link>
        </div>
      </section>
    </main>
  );
}
