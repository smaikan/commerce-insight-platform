import Link from "next/link";

type BenefitIcon = "delivery" | "support" | "return" | "payment";

// Burada referanstaki dört eşit güven bloğunu yalnız belgelenmiş mağaza davranışlarıyla tanımlıyorum.
const STORE_BENEFITS: Array<{
  title: string;
  description: string;
  href: string;
  icon: BenefitIcon;
}> = [
  {
    title: "TESLİMAT SEÇENEKLERİ",
    description: "Aktif kargo seçeneklerini sipariş adımında görebilirsiniz",
    href: "/payment-and-delivery",
    icon: "delivery",
  },
  {
    title: "DESTEK KANALLARI",
    description: "Yayınlanan iletişim bilgilerimizden bize ulaşabilirsiniz",
    href: "#store-contact",
    icon: "support",
  },
  {
    title: "14 GÜN İÇİNDE CAYMA",
    description: "Koşullara tabi cayma ve iade sürecini inceleyebilirsiniz",
    href: "/cancellation-and-refund",
    icon: "return",
  },
  {
    title: "GÜVENLİ ÖDEME",
    description: "Ödeme tutarı yetkili servis yanıtıyla doğrulanır",
    href: "/payment-and-delivery",
    icon: "payment",
  },
];

// Burada footer öncesi fayda satırını referanstaki yatay ritimle, mobilde okunabilir sıraya dönüşecek biçimde sunuyorum.
export function StoreBenefits() {
  return (
    <section className="border-t border-line bg-surface-subtle/55" aria-label="Alışveriş bilgileri">
      <div className="page-shell grid gap-x-10 gap-y-8 py-9 sm:grid-cols-2 sm:py-10 lg:grid-cols-4 lg:gap-x-12">
        {STORE_BENEFITS.map((benefit) => (
          <Link
            key={benefit.title}
            href={benefit.href}
            prefetch={false}
            className="focus-ring group grid grid-cols-[3.25rem_minmax(0,1fr)] items-start gap-5 rounded-md"
          >
            <BenefitIconGraphic icon={benefit.icon} />
            <span className="min-w-0">
              <span className="block text-sm font-bold tracking-[-0.01em] text-brand-950 sm:text-base">
                {benefit.title}
              </span>
              <span className="mt-1.5 block text-sm leading-6 text-ink-muted">
                {benefit.description}
              </span>
            </span>
          </Link>
        ))}
      </div>
    </section>
  );
}

// Burada referanstaki ince çizgili teslimat, destek, iade ve güvenlik simgelerini ek paket olmadan çiziyorum.
function BenefitIconGraphic({ icon }: { icon: BenefitIcon }) {
  const common = "size-11 text-brand-600 transition-colors group-hover:text-brand-950";

  if (icon === "delivery") {
    return (
      <svg aria-hidden="true" viewBox="0 0 48 48" className={common} fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <path d="M7 20h27v16H7zM34 25h5l4 5v6h-9zM13 20l3-8h14l3 8" />
        <circle cx="15" cy="37" r="3" /><circle cx="36" cy="37" r="3" /><path d="M11 27h12" />
      </svg>
    );
  }

  if (icon === "support") {
    return (
      <svg aria-hidden="true" viewBox="0 0 48 48" className={common} fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="24" cy="24" r="18" /><circle cx="24" cy="24" r="10" />
        <path d="m11 11 7 7m19-7-7 7m7 19-7-7m-19 7 7-7" />
      </svg>
    );
  }

  if (icon === "return") {
    return (
      <svg aria-hidden="true" viewBox="0 0 48 48" className={common} fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <path d="M35 13v10H25" /><path d="M35 23a15 15 0 1 0 3 14" /><path d="m35 13 7 5-7 5" />
      </svg>
    );
  }

  return (
    <svg aria-hidden="true" viewBox="0 0 48 48" className={common} fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="24" cy="24" r="18" /><path d="M20 22a4 4 0 1 1 8 0c0 2-1 3-2 4l2 9h-8l2-9c-1-1-2-2-2-4Z" />
    </svg>
  );
}
