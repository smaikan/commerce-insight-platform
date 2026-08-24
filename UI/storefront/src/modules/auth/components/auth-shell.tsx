import Link from "next/link";

import { siteConfig } from "@/lib/site-config";

type AuthShellProps = {
  eyebrow: string;
  title: string;
  description: string;
  children: React.ReactNode;
};

const benefits = [
  ["01", "Siparişlerini tek yerde takip et"],
  ["02", "Adres işlemlerini daha hızlı tamamla"],
  ["03", "Oturumlarını güvenle yönet"],
] as const;

// Burada masaüstünde editoryal iki kolon, mobilde form öncelikli tek kolon kullanan ortak auth kompozisyonunu kuruyorum.
export function AuthShell({ eyebrow, title, description, children }: AuthShellProps) {
  return (
    <main id="main-content" className="relative isolate flex flex-1 overflow-hidden bg-background">
      <div aria-hidden="true" className="pointer-events-none absolute -top-28 right-[8%] -z-10 size-72 rounded-full border border-brand-600/15" />
      <div aria-hidden="true" className="pointer-events-none absolute top-10 right-[14%] -z-10 size-36 rounded-full border border-brand-600/10" />

      <div className="page-shell grid w-full items-stretch py-6 sm:py-8 lg:grid-cols-[minmax(0,0.86fr)_minmax(28rem,1.14fr)] lg:gap-10 lg:py-10 xl:gap-14">
        <section className="order-2 mt-8 border-t border-line pt-8 lg:order-1 lg:mt-0 lg:flex lg:min-h-[42rem] lg:flex-col lg:justify-between lg:border-t-0 lg:border-r lg:pr-10 lg:pt-0 xl:pr-16" aria-labelledby="auth-story-title">
          <div className="max-w-xl">
            <Link href="/" prefetch={false} className="focus-ring inline-flex items-center gap-3 text-xs font-black tracking-[0.18em] text-brand-950 uppercase">
              <span aria-hidden="true" className="inline-block size-2 bg-brand-600" />
              {siteConfig.name}
            </Link>
            <p className="mt-12 text-xs font-bold tracking-[0.16em] text-brand-700 uppercase">Kişisel alışveriş alanın</p>
            <h2 id="auth-story-title" className="mt-4 max-w-lg text-3xl leading-[1.08] font-black tracking-[-0.035em] text-brand-950 sm:text-4xl xl:text-5xl">
              Daha az tekrar, daha düzenli bir alışveriş deneyimi.
            </h2>
          </div>

          <ol className="mt-10 grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
            {benefits.map(([number, label]) => (
              <li key={number} className="flex min-h-16 items-center gap-4 border-t border-line py-3 text-sm font-semibold text-ink">
                <span className="font-mono text-[0.6875rem] tracking-widest text-brand-600" aria-hidden="true">{number}</span>
                {label}
              </li>
            ))}
          </ol>
        </section>

        <section className="order-1 flex items-center justify-center lg:order-2" aria-labelledby="auth-page-title">
          <div className="w-full max-w-xl rounded-2xl border border-line bg-surface px-5 py-7 shadow-panel sm:px-9 sm:py-10 lg:px-11">
            <p className="text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">{eyebrow}</p>
            <h1 id="auth-page-title" className="mt-3 text-3xl leading-tight font-black tracking-[-0.03em] text-brand-950 sm:text-[2.5rem]">{title}</h1>
            <p className="mt-4 max-w-md text-sm leading-6 text-ink-muted sm:text-base">{description}</p>
            <div className="mt-8">{children}</div>
          </div>
        </section>
      </div>
    </main>
  );
}
