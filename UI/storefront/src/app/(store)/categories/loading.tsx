// Burada kategori verisi beklenirken koleksiyon sayfasıyla aynı başlık ve kart geometrisini rezerve ederek yerleşim kaymasını azaltıyorum.
export default function CategoriesLoading() {
  return (
    <main id="main-content" className="flex-1 pb-12 sm:pb-16" aria-busy="true" aria-label="Kategoriler yükleniyor">
      <div className="border-b border-line/80 bg-surface-subtle/55">
        <div className="page-shell py-7 sm:py-8 lg:py-9">
          <div className="h-3 w-24 rounded bg-line/70" />
          <div className="mt-4 h-10 w-64 max-w-full rounded bg-line/70 sm:h-12" />
        </div>
      </div>
      {/* Burada yükleme iskeletini gerçek listeyle aynı mobil, tablet ve masaüstü sütunlarında tutuyorum. */}
      <div className="page-shell grid grid-cols-1 gap-8 pt-6 sm:grid-cols-2 sm:gap-y-9 sm:pt-8 md:grid-cols-3 md:gap-y-10 xl:gap-x-6 xl:pt-9">
        {[0, 1, 2, 3, 4, 5].map((item) => (
          <div key={item}>
            <div className="aspect-[16/10] rounded-lg bg-line/55 sm:aspect-[3/2]" />
            <div className="mt-4 h-6 w-2/3 rounded bg-line/55" />
          </div>
        ))}
      </div>
    </main>
  );
}
