import Link from "next/link";

// Burada bulunamayan ürün kimliğinde operatörü katalog listesine güvenli biçimde geri yönlendiriyorum.
export default function ProductNotFound() {
  return (
    <section className="rounded-xl border border-border bg-surface p-6">
      <h1 className="text-xl font-semibold text-foreground">Ürün bulunamadı</h1>
      <p className="mt-2 text-sm leading-6 text-muted">Ürün silinmiş olabilir veya bağlantıdaki ürün kimliği geçersizdir.</p>
      <Link href="/products" className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Ürün listesine dön</Link>
    </section>
  );
}
