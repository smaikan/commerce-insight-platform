// Burada stok defteri yüklenirken filtre, tablo ve yan panel geometrisini sabit tutuyorum.
export default function StockMovementsLoading() {
  return <div className="mx-auto w-full max-w-screen-2xl" aria-busy="true" aria-label="Stok hareketleri yükleniyor"><div className="mb-5 h-16 max-w-2xl rounded-lg bg-surface-subtle" /><div className="grid gap-5 2xl:grid-cols-[minmax(0,1fr)_20rem]"><div className="h-[620px] rounded-xl border border-border bg-surface" /><div className="h-80 rounded-xl border border-border bg-surface" /></div></div>;
}
