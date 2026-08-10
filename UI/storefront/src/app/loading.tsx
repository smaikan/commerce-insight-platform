// Burada public banner verileri gelirken ana medya oranını koruyup sayfa kaymasını azaltıyorum.
export default function HomeLoading() {
  return (
    <div className="min-h-screen bg-white" aria-busy="true" aria-label="Vitrin yükleniyor">
      <div className="h-16 border-b border-zinc-200/80" />
      <main className="mx-auto w-full max-w-[90rem] px-4 pt-4 sm:px-6 sm:pt-6 lg:px-8">
        <div className="aspect-[16/11] rounded-xl bg-zinc-100 sm:aspect-[16/7] lg:aspect-[21/8]" />
      </main>
    </div>
  );
}
