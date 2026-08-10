import Link from "next/link";

// Burada bulunamayan marka bağlantısından güvenli liste dönüşü sağlıyorum.
export default function BrandNotFound() {
  return (
    <div className="mx-auto max-w-xl rounded-xl border border-border bg-surface px-5 py-10 text-center">
      <h1 className="text-lg font-semibold text-foreground">Marka bulunamadı</h1>
      <p className="mt-2 text-sm text-muted">Marka kaldırılmış veya bağlantı artık geçerli olmayabilir.</p>
      <Link href="/brands" className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white">Markalara dön</Link>
    </div>
  );
}
