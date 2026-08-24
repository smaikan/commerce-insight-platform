export default function AccountingLoading() {
  return (
    <div aria-busy="true" aria-label="Ön muhasebe yükleniyor" className="space-y-4">
      <div className="h-16 max-w-xl animate-pulse rounded-lg bg-surface-subtle" />
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">{[1, 2, 3, 4].map((item) => <div key={item} className="h-32 animate-pulse rounded-xl border border-border bg-surface" />)}</div>
    </div>
  );
}
