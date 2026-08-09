// Burada dashboard metrikleri yüklenirken nihai düzeni koruyan sakin bir iskelet gösteriyorum.
export default function DashboardLoading() {
  return (
    <div className="mx-auto w-full max-w-screen-2xl" aria-busy="true" aria-label="Dashboard yükleniyor">
      <div className="mb-5 h-16 max-w-2xl rounded-lg bg-surface-subtle" />
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 6 }, (_, index) => <div key={index} className="h-32 rounded-xl border border-border bg-surface" />)}
      </div>
      <div className="mt-5 overflow-hidden rounded-xl border border-border bg-surface">
        <div className="h-20 border-b border-border bg-surface-strong" />
        <div className="grid lg:grid-cols-[minmax(0,1fr)_22rem]">
          <div className="h-80 border-b border-border bg-surface-subtle lg:border-b-0 lg:border-r" />
          <div className="h-80 bg-surface-strong" />
        </div>
      </div>
    </div>
  );
}
