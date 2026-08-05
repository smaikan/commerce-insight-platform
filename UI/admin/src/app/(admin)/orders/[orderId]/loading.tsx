// Burada sipariş detayı yüklenirken ana kalem alanı ve yan özet rayının yerleşimini koruyorum.
export default function OrderDetailLoading() {
  return (
    <div className="mx-auto w-full max-w-[1480px]" aria-busy="true" aria-label="Sipariş detayı yükleniyor">
      <div className="mb-6 h-20 max-w-2xl rounded-lg bg-surface-subtle" />
      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_21rem]">
        <div className="space-y-6">
          <div className="h-[430px] rounded-xl border border-border bg-surface-strong" />
          <div className="h-52 rounded-xl border border-border bg-surface-strong" />
        </div>
        <div className="space-y-6">
          {Array.from({ length: 4 }, (_, index) => <div key={index} className="h-48 rounded-xl border border-border bg-surface-strong" />)}
        </div>
      </div>
    </div>
  );
}
