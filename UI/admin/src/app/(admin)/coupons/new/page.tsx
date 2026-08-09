import type { Metadata } from "next";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { CouponForm } from "@/modules/coupons/components/coupon-form";

export const metadata: Metadata = { title: "Kupon oluştur" };

// Burada yalnız API'nin desteklediği iki indirim türü için yeni kupon formunu açıyorum.
export default function NewCouponPage() {
  return <div className="mx-auto w-full max-w-5xl"><PageHeader title="Kupon oluştur" description="Yüzde veya sabit tutar indirimi için kupon kodu ve kullanım koşullarını belirleyin." backHref="/coupons" /><div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_18rem]"><CouponForm /><aside className="h-fit rounded-xl border border-border bg-surface p-4"><p className="text-xs font-bold uppercase tracking-[0.08em] text-muted">Nasıl çalışır?</p><ol className="mt-3 space-y-3 text-sm leading-6 text-muted"><li><span className="font-semibold text-foreground">1.</span> Müşteri checkout’ta tek bir kupon kodu girer.</li><li><span className="font-semibold text-foreground">2.</span> Backend aktiflik, tarih, limit ve sepet koşullarını doğrular.</li><li><span className="font-semibold text-foreground">3.</span> İndirim ve toplam tutar backend’de hesaplanır.</li></ol></aside></div></div>;
}
