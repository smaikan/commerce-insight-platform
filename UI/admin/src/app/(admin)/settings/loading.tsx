// Burada ayarlar rotaları yüklenirken son yerleşime yakın ve kaymayan bir iskelet gösteriyorum.
export default function SettingsLoading() {
  return <div className="mx-auto w-full max-w-screen-2xl" aria-busy="true" aria-label="Ayarlar yükleniyor"><div className="mb-4 h-14 w-full max-w-xl animate-pulse rounded-lg bg-surface-subtle" /><div className="grid gap-4 lg:grid-cols-[220px_minmax(0,1fr)]"><div className="h-64 animate-pulse rounded-xl border border-border bg-surface" /><div className="grid gap-4 xl:grid-cols-2">{Array.from({ length: 4 }, (_, index) => <div key={index} className="h-56 animate-pulse rounded-xl border border-border bg-surface" />)}</div></div></div>;
}
