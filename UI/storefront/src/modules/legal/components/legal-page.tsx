import Link from "next/link";
import type { ReactNode } from "react";

export type LegalSection = {
  id: string;
  title: string;
  content: ReactNode;
};

type LegalPageProps = {
  eyebrow: string;
  title: string;
  summary: string;
  sections: LegalSection[];
  notice?: ReactNode;
};

// Burada uzun yasal metinleri masaüstünde içindekiler, mobilde doğal okuma sırasıyla erişilebilir ve sakin bir düzende sunuyorum.
export function LegalPage({ eyebrow, title, summary, sections, notice }: LegalPageProps) {
  return (
    <main id="main-content" className="flex-1 pb-16 sm:pb-20">
      <header className="border-b border-line bg-surface">
        <div className="page-shell py-9 sm:py-12 lg:py-14">
          <nav className="hidden items-center gap-2 text-xs text-ink-muted sm:flex" aria-label="Sayfa yolu">
            <Link href="/" prefetch={false} className="focus-ring hover:text-brand-700">Ana sayfa</Link>
            <span aria-hidden="true">/</span>
            <span aria-current="page">{title}</span>
          </nav>
          <div className="mt-0 max-w-3xl sm:mt-7">
            <p className="text-xs font-bold tracking-[0.12em] text-brand-700 uppercase">{eyebrow}</p>
            <h1 className="mt-3 text-3xl font-black tracking-[-0.035em] text-brand-950 sm:text-4xl">{title}</h1>
            <p className="mt-4 max-w-2xl text-sm leading-7 text-ink-muted sm:text-base">{summary}</p>
            <p className="mt-4 text-xs font-semibold text-ink-muted">Son güncelleme: 13 Ağustos 2026</p>
          </div>
        </div>
      </header>

      <div className="page-shell grid items-start gap-10 pt-8 lg:grid-cols-[15rem_minmax(0,46rem)] lg:justify-center lg:gap-16 lg:pt-12">
        <aside className="hidden lg:block lg:sticky lg:top-28" aria-label="Sayfa içeriği">
          <p className="text-xs font-bold tracking-[0.1em] text-ink uppercase">İçindekiler</p>
          <nav className="mt-3 border-l border-line">
            {sections.map((section, index) => (
              <a key={section.id} href={`#${section.id}`} className="focus-ring flex min-h-10 items-center gap-3 border-l-2 border-transparent py-2 pr-2 pl-4 text-sm leading-5 text-ink-muted hover:border-brand-600 hover:text-brand-700">
                <span className="text-xs tabular-nums text-ink-muted" aria-hidden="true">{String(index + 1).padStart(2, "0")}</span>
                {section.title}
              </a>
            ))}
          </nav>
        </aside>

        <article className="min-w-0">
          {notice ? (
            <div className="mb-8 border-l-4 border-brand-600 bg-surface-subtle px-4 py-4 text-sm leading-6 text-ink sm:px-5">
              {notice}
            </div>
          ) : null}

          {sections.map((section, index) => (
            <section key={section.id} id={section.id} className="scroll-mt-28 border-b border-line py-8 first:pt-0 last:border-b-0 last:pb-0" aria-labelledby={`${section.id}-title`}>
              <div className="flex items-start gap-3">
                <span className="pt-1 text-xs font-bold tabular-nums text-brand-700" aria-hidden="true">{String(index + 1).padStart(2, "0")}</span>
                <h2 id={`${section.id}-title`} className="text-xl font-bold tracking-[-0.02em] text-brand-950 sm:text-2xl">{section.title}</h2>
              </div>
              <div className="legal-copy mt-5 pl-0 sm:pl-8">{section.content}</div>
            </section>
          ))}
        </article>
      </div>
    </main>
  );
}
