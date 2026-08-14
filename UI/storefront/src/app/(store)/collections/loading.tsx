// Burada koleksiyon verisi beklenirken son mozaik geometrisini rezerve ederek yerleşim kaymasını azaltıyorum.
export default function CollectionsLoading() {
  return (
    <main id="main-content" className="flex-1 pb-16" aria-busy="true" aria-label="Koleksiyonlar yükleniyor">
      <div className="border-b border-line/80 bg-surface-subtle/55">
        <div className="page-shell py-10 sm:py-14 lg:py-16">
          <div className="h-3 w-24 rounded bg-line/70" />
          <div className="mt-4 h-10 w-64 max-w-full rounded bg-line/70 sm:h-12" />
        </div>
      </div>
      <div className="page-shell grid grid-cols-1 gap-9 pt-8 sm:grid-cols-2 sm:pt-10 xl:grid-cols-3 xl:pt-12">
        {[0, 1, 2, 3].map((item) => (
          <div key={item} className={item === 0 || item === 3 ? "xl:col-span-2" : "xl:col-span-1"}>
            <div className={`rounded-xl bg-line/55 ${item === 0 || item === 3 ? "aspect-[16/10] sm:aspect-[3/2] xl:aspect-[3/1]" : "aspect-[16/10] sm:aspect-[3/2]"}`} />
            <div className="mt-4 h-6 w-2/3 rounded bg-line/55" />
          </div>
        ))}
      </div>
    </main>
  );
}
