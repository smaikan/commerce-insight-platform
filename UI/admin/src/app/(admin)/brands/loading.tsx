// Burada marka listesi yüklenirken nihai tablo geometrisine yakın sade bir iskelet gösteriyorum.
export default function BrandsLoading() {
  return (
    <div className="mx-auto w-full max-w-screen-2xl" aria-busy="true" aria-label="Markalar yükleniyor">
      <div className="mb-5 flex items-center justify-between gap-4">
        <div className="space-y-2"><div className="h-7 w-36 rounded bg-surface-subtle" /><div className="h-4 w-80 max-w-full rounded bg-surface-subtle" /></div>
        <div className="h-10 w-36 rounded-lg bg-surface-subtle" />
      </div>
      <div className="overflow-hidden rounded-xl border border-border bg-surface">
        <div className="h-10 border-b border-border bg-surface-subtle" />
        {Array.from({ length: 5 }, (_, index) => <div key={index} className="h-16 border-b border-border last:border-b-0" />)}
      </div>
    </div>
  );
}
