// Burada müşteri listesi yüklenirken filtre ve tablo geometrisini koruyan sakin bir iskelet gösteriyorum.
export default function CustomersLoading() {
  return (
    <div className="w-full" aria-busy="true" aria-label="Müşteriler yükleniyor">
      <div className="mb-6 h-16 max-w-2xl rounded-lg bg-surface-subtle" />
      <div className="overflow-hidden rounded-xl border border-border bg-surface">
        <div className="h-24 border-b border-border bg-surface-subtle" />
        <div className="h-11 border-b border-border bg-surface-subtle/80" />
        <div className="divide-y divide-border">
          {Array.from({ length: 8 }, (_, index) => (
            <div key={index} className="flex items-center gap-3 px-5 py-3.5">
              <div className="size-9 shrink-0 animate-pulse rounded-full bg-surface-subtle" />
              <div className="flex-1 space-y-2">
                <div className="h-4 w-40 animate-pulse rounded bg-surface-subtle" />
                <div className="h-3 w-24 animate-pulse rounded bg-surface-subtle" />
              </div>
              <div className="h-4 w-28 animate-pulse rounded bg-surface-subtle" />
              <div className="h-5 w-14 animate-pulse rounded-md bg-surface-subtle" />
              <div className="h-5 w-12 animate-pulse rounded-md bg-surface-subtle" />
              <div className="h-4 w-28 animate-pulse rounded bg-surface-subtle" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
