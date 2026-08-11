import Link from "next/link";

import { CartIndicator } from "@/components/storefront/cart-indicator";
import { MobileNavigation } from "@/components/storefront/mobile-navigation";
import { siteConfig } from "@/lib/site-config";

// Burada masaüstü ve mobilde aynı bilgi mimarisini koruyan hafif Storefront üst alanını oluşturuyorum.
export function SiteHeader() {
  return (
    <>
      <div className="bg-brand-950 px-3 py-2 text-center text-[0.625rem] font-semibold tracking-[0.1em] text-white uppercase sm:px-4 sm:text-[0.6875rem] sm:tracking-[0.12em]">
        {siteConfig.name} · Online mağaza
      </div>
      <header className="relative sticky top-0 z-40 border-b border-line/80 bg-surface">
        <div className="page-shell grid min-h-16 grid-cols-[minmax(0,1fr)_auto] items-center gap-4 sm:min-h-18 lg:grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)]">
          <div className="flex min-w-0 items-center gap-1 sm:gap-2">
            <MobileNavigation currency={siteConfig.currency} siteName={siteConfig.name} />
            <Link
              href="/"
              className="focus-ring truncate text-base font-black tracking-[0.14em] text-brand-950 sm:text-lg sm:tracking-[0.16em]"
              aria-label={`${siteConfig.name} ana sayfa`}
            >
              {siteConfig.name}
            </Link>
          </div>

          <nav className="hidden items-center gap-7 text-sm font-semibold text-ink-muted lg:flex" aria-label="Ana navigasyon">
            <Link className="nav-link" href="/">Ana sayfa</Link>
            <Link className="nav-link" href="/products">Ürünler</Link>
          </nav>

          <div className="flex items-center justify-end gap-2 text-sm font-semibold">
            <span className="hidden text-xs tracking-wide text-ink-muted lg:inline">TR · {siteConfig.currency}</span>
            <CartIndicator />
          </div>
        </div>
      </header>
    </>
  );
}
