import Link from "next/link";

import { siteConfig } from "@/lib/site-config";

// Burada doğrulanmamış kurumsal iddialar eklemeden sade Storefront alt alanını sunuyorum.
export function SiteFooter() {
  return (
    <footer className="mt-auto border-t border-line bg-surface-subtle">
      <div className="page-shell grid gap-8 py-10 sm:grid-cols-[minmax(0,1fr)_auto] sm:gap-12 sm:py-12">
        <div className="max-w-sm">
          <Link
            href="/"
            className="focus-ring inline-block text-lg font-black tracking-[0.16em] text-brand-950"
            aria-label={`${siteConfig.name} ana sayfa`}
          >
            {siteConfig.name}
          </Link>
          <p className="mt-3 text-sm leading-6 text-ink-muted">Online mağaza</p>
        </div>

        <div className="sm:min-w-40">
          <h2 className="text-xs font-bold tracking-[0.1em] text-ink uppercase">Keşfet</h2>
          <nav className="mt-2 flex flex-col items-start" aria-label="Alt navigasyon">
            <Link className="nav-link inline-flex min-h-11 items-center" href="/">Ana sayfa</Link>
            <Link className="nav-link inline-flex min-h-11 items-center" href="/products">Ürünler</Link>
          </nav>
        </div>
      </div>

      <div className="border-t border-line/80">
        <div className="page-shell flex flex-col gap-2 py-4 text-xs text-ink-muted sm:flex-row sm:items-center sm:justify-between">
          <p>© {new Date().getFullYear()} {siteConfig.name}</p>
          <p>TR · {siteConfig.currency}</p>
        </div>
      </div>
    </footer>
  );
}
