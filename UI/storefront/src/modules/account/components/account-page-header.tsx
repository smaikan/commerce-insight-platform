type AccountPageHeaderProps = {
  eyebrow: string;
  title: string;
  description: string;
  action?: React.ReactNode;
};

// Burada hesap sayfalarının başlık, açıklama ve birincil aksiyon hizasını ortaklaştırıyorum.
export function AccountPageHeader({ eyebrow, title, description, action }: AccountPageHeaderProps) {
  return (
    <header className="border-b border-line pb-6">
      <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">{eyebrow}</p>
          <h1 className="mt-3 text-3xl font-black tracking-[-0.025em] text-brand-950">{title}</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-ink-muted">{description}</p>
        </div>
        {action ? <div className="shrink-0">{action}</div> : null}
      </div>
    </header>
  );
}
