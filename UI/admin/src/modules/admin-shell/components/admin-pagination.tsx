import Link from "next/link";

type HiddenField = {
  name: string;
  value: string | number | boolean;
};

type AdminPaginationProps = {
  action: string;
  ariaLabel: string;
  buildHref: (pageNumber: number) => string;
  hiddenFields?: readonly HiddenField[];
  itemLabel: string;
  pageNumber: number;
  pageParam?: string;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

// Burada admin listelerinin sonuç özetini, sıralı gezinmesini ve doğrudan sayfa atlama formunu tek tutarlı düzende sunuyorum.
export function AdminPagination({
  action,
  ariaLabel,
  buildHref,
  hiddenFields = [],
  itemLabel,
  pageNumber,
  pageParam = "pageNumber",
  pageSize,
  totalCount,
  totalPages,
}: AdminPaginationProps) {
  const firstItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const lastItem = Math.min(pageNumber * pageSize, totalCount);
  const safeTotalPages = Math.max(totalPages, 1);
  const pageInputId = `${ariaLabel.toLocaleLowerCase("tr-TR").replace(/[^a-z0-9]+/g, "-")}-page-number`;

  return (
    <footer className="border-t border-border bg-surface-subtle/40 px-4 py-3 text-sm sm:px-5">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <p className="whitespace-nowrap text-muted">
          <span className="font-semibold tabular-nums text-foreground">{firstItem}-{lastItem}</span>{" "}
          / <span className="tabular-nums">{totalCount}</span> {itemLabel}
        </p>

        {safeTotalPages > 1 ? (
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between lg:justify-end">
            <nav aria-label={ariaLabel} className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-2 sm:flex">
              <PageLink disabled={pageNumber <= 1} href={buildHref(pageNumber - 1)} direction="previous">
                Önceki
              </PageLink>
              <span aria-current="page" className="min-h-11 min-w-24 rounded-lg bg-surface px-3 py-3 text-center font-semibold tabular-nums text-foreground sm:min-h-9 sm:py-2">
                Sayfa {pageNumber} / {safeTotalPages}
              </span>
              <PageLink disabled={pageNumber >= safeTotalPages} href={buildHref(pageNumber + 1)} direction="next">
                Sonraki
              </PageLink>
            </nav>

            <form action={action} method="get" className="flex items-center gap-2 border-border-strong sm:border-l sm:pl-3">
              {hiddenFields.map((field) => (
                <input key={field.name} type="hidden" name={field.name} value={String(field.value)} />
              ))}
              <label htmlFor={pageInputId} className="whitespace-nowrap text-xs font-semibold text-muted">
                Sayfaya git
              </label>
              <input
                id={pageInputId}
                name={pageParam}
                type="number"
                inputMode="numeric"
                min={1}
                max={safeTotalPages}
                defaultValue={pageNumber}
                className="min-h-11 w-16 rounded-lg border border-border-strong bg-surface-strong px-2 text-center text-sm font-semibold tabular-nums text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 sm:min-h-9"
              />
              <button type="submit" className="inline-flex min-h-11 cursor-pointer items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground transition-colors hover:bg-surface-subtle sm:min-h-9">
                Git
              </button>
            </form>
          </div>
        ) : null}
      </div>
    </footer>
  );
}

// Burada sınır sayfalarındaki gezinme eylemlerini odağa girmeyen, bağlantı gibi davranmayan okunabilir durumlarla ayırıyorum.
function PageLink({
  children,
  direction,
  disabled,
  href,
}: {
  children: React.ReactNode;
  direction: "previous" | "next";
  disabled: boolean;
  href: string;
}) {
  const className = "inline-flex min-h-11 items-center justify-center gap-1 rounded-lg border px-3 font-semibold sm:min-h-9";
  const content = (
    <>
      {direction === "previous" ? <Chevron direction="previous" /> : null}
      <span>{children}</span>
      {direction === "next" ? <Chevron direction="next" /> : null}
    </>
  );

  return disabled ? (
    <span aria-disabled="true" className={`${className} cursor-not-allowed border-border bg-surface text-muted`}>
      {content}
    </span>
  ) : (
    <Link href={href} className={`${className} cursor-pointer border-border-strong bg-surface-strong text-foreground transition-colors hover:bg-surface-subtle`}>
      {content}
    </Link>
  );
}

// Burada ileri ve geri yönlerini mevcut ikon diliyle uyumlu küçük bir ok işaretiyle destekliyorum.
function Chevron({ direction }: { direction: "previous" | "next" }) {
  return (
    <svg aria-hidden="true" viewBox="0 0 20 20" className="size-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d={direction === "previous" ? "m12 5-5 5 5 5" : "m8 5 5 5-5 5"} />
    </svg>
  );
}
