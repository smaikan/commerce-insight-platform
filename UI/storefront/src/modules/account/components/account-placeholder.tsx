import Link from "next/link";

type AccountPlaceholderProps = {
  eyebrow: string;
  title: string;
  description: string;
  emptyTitle: string;
  emptyDescription: string;
  action?: { href: string; label: string };
  disabledAction?: string;
};

// Burada henüz API entegrasyonu yapılmamış hesap sayfalarını sahte veri üretmeden tamamlanmış bir frontend durumu olarak sunuyorum.
export function AccountPlaceholder({ eyebrow, title, description, emptyTitle, emptyDescription, action, disabledAction }: AccountPlaceholderProps) {
  return (
    <section aria-labelledby="account-page-title">
      <p className="text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">{eyebrow}</p>
      <div className="mt-3 flex flex-col gap-4 border-b border-line pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 id="account-page-title" className="text-3xl font-black tracking-[-0.025em] text-brand-950">{title}</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-ink-muted">{description}</p>
        </div>
        {disabledAction ? (
          <button type="button" disabled aria-describedby="account-development-note" className="min-h-11 shrink-0 cursor-not-allowed border border-line bg-surface-subtle px-4 text-sm font-bold text-ink-muted">
            {disabledAction}
          </button>
        ) : null}
      </div>

      <div className="mt-8 border border-line bg-surface px-6 py-10 text-center sm:px-10 sm:py-14">
        <span className="inline-flex size-11 items-center justify-center border border-line bg-surface-subtle text-brand-700" aria-hidden="true">
          <svg viewBox="0 0 24 24" className="size-5" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round"><path d="M6 8h12M6 12h8M6 16h5" /><path d="M4 4h16v16H4z" /></svg>
        </span>
        <h2 className="mt-4 text-lg font-black text-ink">{emptyTitle}</h2>
        <p className="mx-auto mt-2 max-w-lg text-sm leading-6 text-ink-muted">{emptyDescription}</p>
        {action ? <Link href={action.href} prefetch={false} className="focus-ring mt-6 inline-flex min-h-11 items-center bg-brand-950 px-5 text-sm font-bold text-white hover:bg-brand-700">{action.label}</Link> : null}
      </div>

      <p id="account-development-note" className="mt-4 text-xs leading-5 text-ink-muted">
        Bu alanın kullanıcı verileriyle bağlantısı geliştirme aşamasında.
      </p>
    </section>
  );
}
