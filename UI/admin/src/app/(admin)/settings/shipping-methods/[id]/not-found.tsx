import Link from "next/link";

// Burada silinmiş veya geçersiz kargo yöntemi kimliği için güvenli liste dönüşü sağlıyorum.
export default function ShippingMethodNotFound() {
  return <div className="mx-auto max-w-xl rounded-xl border border-border bg-surface px-5 py-10 text-center"><h1 className="text-lg font-semibold text-foreground">Kargo yöntemi bulunamadı</h1><p className="mt-2 text-sm text-muted">Kayıt kaldırılmış veya bağlantı artık geçerli olmayabilir.</p><Link href="/settings/shipping-methods" className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white">Listeye dön</Link></div>;
}
