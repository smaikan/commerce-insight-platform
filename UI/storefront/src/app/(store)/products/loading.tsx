// Burada katalog yüklenirken 4:5 ürün medyası ve son içerik geometrisini koruyorum.
export default function ProductsLoading() {
  return (
    <main id="main-content" className="page-shell max-w-[80rem] flex-1 py-8 sm:py-12" aria-busy="true" aria-label="Ürünler yükleniyor">
      <div className="h-3 w-20 rounded bg-line" />
      <div className="mt-4 h-10 w-64 max-w-full rounded bg-line" />
      <div className="mt-4 h-5 w-96 max-w-full rounded bg-line/80" />
      <div className="mt-9 flex h-14 items-center justify-between border-y border-line">
        <div className="h-4 w-24 rounded bg-line" />
        <div className="size-5 rounded bg-line/70" />
      </div>
      <div className="h-16 border-b border-line" />
      <div className="mt-7 grid grid-cols-2 gap-x-3 gap-y-8 sm:gap-x-4 md:grid-cols-3 lg:grid-cols-4 lg:gap-x-6">
        {Array.from({ length: 8 }, (_, index) => (
          <div key={index} aria-hidden="true">
            <div className="aspect-[4/5] rounded-xl bg-line/70" />
            <div className="mt-4 h-3 w-1/3 rounded bg-line" />
            <div className="mt-2 h-4 w-full rounded bg-line/80" />
            <div className="mt-1.5 h-4 w-3/4 rounded bg-line/70" />
            <div className="mt-4 h-5 w-1/2 rounded bg-line" />
            <div className="mt-2 h-3 w-2/5 rounded bg-line/70" />
          </div>
        ))}
      </div>
    </main>
  );
}
