// Burada public banner verileri gelirken ana medya oranını koruyup sayfa kaymasını azaltıyorum.
export default function HomeLoading() {
  return (
    <main id="main-content" className="page-shell flex-1 pt-4 sm:pt-6" aria-busy="true" aria-label="Vitrin yükleniyor">
      <div className="aspect-[16/11] rounded-xl bg-line/70 sm:aspect-[16/7] lg:aspect-[21/8]" />
    </main>
  );
}
