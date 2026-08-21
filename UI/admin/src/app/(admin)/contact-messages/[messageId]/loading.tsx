// Burada mesaj detail iki kolon geometrisini yükleme sırasında koruyorum.
export default function ContactMessageDetailLoading() {
  return <div aria-busy="true" aria-label="İletişim mesajı yükleniyor" className="mx-auto w-full max-w-[1480px]"><div className="mb-6 h-16 max-w-xl animate-pulse rounded-lg bg-surface-subtle motion-reduce:animate-none" /><div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_21rem]"><div className="space-y-5"><div className="h-72 animate-pulse rounded-xl bg-surface-subtle motion-reduce:animate-none" /><div className="h-96 animate-pulse rounded-xl bg-surface-subtle motion-reduce:animate-none" /></div><div className="h-[34rem] animate-pulse rounded-xl border border-border bg-surface motion-reduce:animate-none" /></div></div>;
}
