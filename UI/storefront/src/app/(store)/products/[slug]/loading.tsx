// Burada dengelenmiş ana 4:5 görsel ve sağ bilgi panelinin son geometrisini yükleme boyunca koruyorum.
export default function ProductLoading() {
  return (
    <main id="main-content" className="page-shell flex-1 py-6 sm:py-8 lg:py-10" aria-busy="true" aria-label="Ürün yükleniyor">
      <div className="mb-8 hidden h-3 w-48 rounded bg-line md:block" />
      <div className="grid gap-8 lg:grid-cols-[minmax(0,1.1fr)_minmax(24rem,0.9fr)] lg:gap-x-10 xl:gap-x-14">
        <div className="min-w-0 lg:col-start-1 lg:row-start-1">
          <div className="-mx-4 aspect-[4/5] w-[calc(100%+2rem)] bg-line/70 sm:mx-auto sm:w-full sm:max-w-[34rem] sm:rounded-2xl" />
          <div className="-mx-4 flex h-14 items-center justify-center gap-3 border-b border-line bg-surface sm:mx-auto sm:max-w-[34rem] lg:hidden">
            <span className="size-2.5 rounded-full bg-line" />
            <span className="size-2 rounded-full bg-line/70" />
          </div>
        </div>
        <div className="lg:col-start-2 lg:row-start-1">
          <div className="h-3 w-28 rounded bg-line" />
          <div className="mt-5 h-10 w-full rounded bg-line" />
          <div className="mt-3 h-10 w-3/4 rounded bg-line/80" />
          <div className="mt-8 h-20 border-y border-line" />
          <div className="mt-6 space-y-3">
            <div className="h-20 rounded-xl bg-line/60" />
            <div className="h-20 rounded-xl bg-line/60" />
          </div>
        </div>
      </div>
    </main>
  );
}
