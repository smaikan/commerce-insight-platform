import Link from "next/link";

export function PageHeader({
  title,
  description,
  actions,
  backHref,
}: {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  backHref?: string;
}) {
  return (
    <header className="mb-6 flex flex-col gap-4 border-l-4 border-primary pl-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="min-w-0">
        {backHref ? (
          <Link href={backHref} className="mb-1 inline-flex min-h-8 items-center text-sm font-medium text-primary hover:text-primary-hover">
            ← Listeye dön
          </Link>
        ) : null}
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">{title}</h1>
        {description ? (
          <p className="mt-1 max-w-3xl text-sm leading-6 text-muted">{description}</p>
        ) : null}
      </div>
      {actions ? <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div> : null}
    </header>
  );
}
