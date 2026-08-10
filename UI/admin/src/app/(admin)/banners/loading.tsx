// Burada altı bağımsız bölüm yüklenirken nihai yönetim yüzeylerinin geometrisini koruyorum.
export default function BannersLoading() {
  return (
    <div className="mx-auto w-full max-w-screen-2xl" aria-busy="true" aria-label="Bannerlar yükleniyor">
      <div className="mb-4 space-y-2 border-l-4 border-primary pl-3">
        <div className="h-7 w-36 rounded bg-surface-subtle" />
        <div className="h-4 w-[32rem] max-w-full rounded bg-surface-subtle" />
      </div>
      <div className="grid gap-4 xl:grid-cols-2">
        {Array.from({ length: 6 }, (_, index) => (
          <section key={index} className="rounded-xl border border-border bg-surface p-4 sm:p-5">
            <div className="h-10 border-b border-border" />
            <div className="mt-4 h-28 rounded-lg bg-surface-subtle" />
          </section>
        ))}
      </div>
    </div>
  );
}
