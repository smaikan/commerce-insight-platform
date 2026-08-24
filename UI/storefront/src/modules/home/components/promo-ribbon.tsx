import Link from "next/link";

// Burada mağazanın öne çıkan 4 temel alışveriş avantajını şık bir şerit olarak sunuyorum.
export function PromoRibbon() {
  const highlights = [
    {
      icon: (
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 shrink-0 text-brand-600" fill="none" stroke="currentColor" strokeWidth="1.75">
          <path d="M5 18h14a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2Z" />
          <path d="M10 6 8.5 3.5a1 1 0 0 0-.87-.5H4" />
          <circle cx="7.5" cy="18.5" r="2.5" />
          <circle cx="16.5" cy="18.5" r="2.5" />
        </svg>
      ),
      title: "Ücretsiz Kargo",
      subtitle: "1.000 TL üzeri tüm siparişlerde",
      href: "/payment-and-delivery",
    },
    {
      icon: (
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 shrink-0 text-brand-600" fill="none" stroke="currentColor" strokeWidth="1.75">
          <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
          <path d="m9 12 2 2 4-4" />
        </svg>
      ),
      title: "Güvenli Alışveriş",
      subtitle: "256-bit SSL & Iyzico altyapısı",
      href: "/payment-and-delivery",
    },
    {
      icon: (
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 shrink-0 text-brand-600" fill="none" stroke="currentColor" strokeWidth="1.75">
          <path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />
          <path d="M3 3v5h5" />
        </svg>
      ),
      title: "Kolay İade & Değişim",
      subtitle: "14 gün içinde koşulsuz cayma hakkı",
      href: "/cancellation-and-refund",
    },
    {
      icon: (
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 shrink-0 text-brand-600" fill="none" stroke="currentColor" strokeWidth="1.75">
          <circle cx="12" cy="12" r="10" />
          <polyline points="12 6 12 12 16 14" />
        </svg>
      ),
      title: "Hızlı Sevkiyat",
      subtitle: "Hafta içi saat 15:00'e kadar aynı gün",
      href: "/payment-and-delivery",
    },
  ];

  return (
    <section aria-label="Alışveriş ayrıcalıkları" className="border-y border-line/60 bg-surface/80 backdrop-blur-xs py-4 sm:py-5">
      <div className="home-shell grid grid-cols-2 gap-4 sm:gap-6 lg:grid-cols-4">
        {highlights.map((item) => (
          <Link
            key={item.title}
            href={item.href}
            prefetch={false}
            className="focus-ring group flex items-center gap-3 p-1.5 rounded-lg transition-colors hover:bg-surface-subtle/60"
          >
            <div className="flex size-10 items-center justify-center rounded-lg bg-surface-subtle border border-line/70 group-hover:border-brand-600 transition-colors">
              {item.icon}
            </div>
            <div className="min-w-0">
              <p className="text-xs sm:text-sm font-bold tracking-tight text-ink group-hover:text-brand-700 transition-colors">
                {item.title}
              </p>
              <p className="text-[0.6875rem] sm:text-xs text-ink-muted truncate">
                {item.subtitle}
              </p>
            </div>
          </Link>
        ))}
      </div>
    </section>
  );
}
