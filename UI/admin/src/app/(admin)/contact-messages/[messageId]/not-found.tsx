import Link from "next/link";

// Burada silinmiş veya geçersiz detail kimliğini güvenli gelen kutusu dönüşüyle karşılıyorum.
export default function ContactMessageNotFound() {
  return <section className="rounded-xl border border-border bg-surface p-6"><h1 className="text-xl font-semibold text-foreground">İletişim mesajı bulunamadı</h1><p className="mt-2 text-sm leading-6 text-muted">Kayıt kaldırılmış veya bağlantı geçersiz olabilir.</p><Link href="/contact-messages" className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">Gelen kutusuna dön</Link></section>;
}
