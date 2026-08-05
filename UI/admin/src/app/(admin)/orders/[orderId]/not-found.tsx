import Link from "next/link";

// Burada bulunamayan sipariş kimliğinde operatörü sipariş listesine güvenli biçimde geri yönlendiriyorum.
export default function OrderNotFound() {
  return (
    <section className="rounded-xl border border-border bg-surface p-6">
      <h1 className="text-xl font-semibold text-foreground">Sipariş bulunamadı</h1>
      <p className="mt-2 text-sm leading-6 text-muted">Sipariş mevcut olmayabilir veya bağlantıdaki kimlik geçersizdir.</p>
      <Link href="/orders" className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Sipariş listesine dön</Link>
    </section>
  );
}
