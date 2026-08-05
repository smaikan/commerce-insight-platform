// Burada ürün listesi yüklenirken son tablo geometrisine yakın ve sakin bir iskelet gösteriyorum.
export default function ProductsLoading() {
  return (
    <div className="w-full" aria-busy="true" aria-label="Ürünler yükleniyor">
      <div className="mb-6 h-16 max-w-xl rounded-lg bg-surface-subtle" />
      <div className="overflow-hidden rounded-xl border border-border bg-surface">
        <div className="h-32 border-b border-border bg-surface-subtle" />
        <div className="divide-y divide-border">
          {Array.from({ length: 6 }, (_, index) => (
            <div key={index} className="h-16 bg-surface-strong" />
          ))}
        </div>
      </div>
    </div>
  );
}
