// Burada gelen kutusunun tablo geometrisini koruyan, hareket zorunluluğu yaratmayan yükleme iskeletini gösteriyorum.
export default function ContactMessagesLoading() {
  return <div className="w-full" aria-busy="true" aria-label="İletişim mesajları yükleniyor"><div className="mb-6 h-16 max-w-xl animate-pulse rounded-lg bg-surface-subtle motion-reduce:animate-none" /><div className="overflow-hidden rounded-xl border border-border bg-surface"><div className="h-36 animate-pulse border-b border-border bg-surface-subtle motion-reduce:animate-none" /><div className="space-y-px bg-border">{Array.from({ length: 6 }, (_, index) => <div key={index} className="h-20 animate-pulse bg-surface-strong motion-reduce:animate-none" />)}</div></div></div>;
}
