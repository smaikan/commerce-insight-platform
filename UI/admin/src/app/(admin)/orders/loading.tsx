// Burada sipariş listesi yüklenirken filtre ve tablo geometrisini koruyan sakin bir iskelet gösteriyorum.
export default function OrdersLoading() {
  return (
    <div className="w-full" aria-busy="true" aria-label="Siparişler yükleniyor">
      <div className="mb-6 h-16 max-w-2xl rounded-lg bg-surface-subtle" />
      <div className="overflow-hidden rounded-xl border border-border bg-surface">
        <div className="h-28 border-b border-border bg-surface-subtle" />
        <div className="h-11 border-b border-border bg-surface-subtle/80" />
        <div className="divide-y divide-border">
          {Array.from({ length: 7 }, (_, index) => <div key={index} className="h-[68px] bg-surface-strong" />)}
        </div>
      </div>
    </div>
  );
}
