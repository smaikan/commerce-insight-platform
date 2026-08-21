import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";

export const metadata: Metadata = {
  title: "Google Analytics 4 | Pazarlama",
  description: "Google Analytics 4 entegrasyonu ve e-ticaret analitikleri.",
};

export default async function GoogleAnalyticsPage() {
  await requireAdminPageSession("/marketing/google-analytics");

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title="Google Analytics 4"
        description="Mağazanızın ziyaretçi trafiğini, ürün görüntülemelerini ve e-ticaret dönüşüm hunisini Google Analytics ile izleyin."
      />

      {/* Geliştirme Aşamasında Bilgilendirme Kartı */}
      <div className="rounded-2xl border border-amber-200 bg-amber-50/70 p-6 text-amber-950 shadow-sm sm:p-8">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-start gap-4">
            <div className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-amber-500 text-white shadow-sm">
              <svg className="size-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
              </svg>
            </div>
            <div>
              <div className="inline-flex items-center gap-2 rounded-full border border-amber-300 bg-amber-100/80 px-2.5 py-0.5 text-xs font-bold text-amber-900">
                <span className="size-2 rounded-full bg-amber-600 animate-pulse" />
                Geliştirme Aşamasında
              </div>
              <h2 className="mt-2 text-xl font-bold tracking-tight text-amber-950 sm:text-2xl">
                Google Analytics 4 Entegrasyonu Çok Yakında
              </h2>
              <p className="mt-1.5 text-sm leading-relaxed text-amber-900/80 max-w-3xl">
                Bu modül üzerinden Google Analytics 4 (GA4) Ölçüm Kimliğinizi (Measurement ID) tek tıkla bağlayabilecek, gerçek zamanlı kullanıcı akışlarını, ürün/kategori bazlı görüntüleme metriklerini ve sepet-ödeme dönüşüm adımlarını doğrudan yönetim panelinizden izleyebileceksiniz.
              </p>
            </div>
          </div>
          <div className="shrink-0">
            <Link
              href="/dashboard"
              className="inline-flex min-h-10 items-center justify-center rounded-xl bg-amber-900 px-4 text-sm font-semibold text-white shadow-sm hover:bg-amber-950 transition-colors"
            >
              Genel Bakışa Dön
            </Link>
          </div>
        </div>
      </div>

      {/* Planlanan Özellikler Izgarası */}
      <div className="mt-8">
        <h3 className="text-base font-bold text-foreground">Planlanan Özellikler ve Metrikler</h3>
        <p className="mt-1 text-xs text-muted">
          GA4 entegrasyonu tamamlandığında aktif hale gelecek yetenekler.
        </p>

        <div className="mt-4 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          <div className="rounded-xl border border-border bg-surface p-5 shadow-sm">
            <div className="flex size-10 items-center justify-center rounded-lg bg-blue-50 text-blue-700">
              <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
            </div>
            <h4 className="mt-3 text-sm font-bold text-foreground">E-ticaret Olayları</h4>
            <p className="mt-1 text-xs leading-relaxed text-muted">
              `view_item`, `add_to_cart`, `begin_checkout` ve `purchase` olayları GA4 Enhanced Ecommerce standartlarına uygun olarak otomatik iletilecektir.
            </p>
          </div>

          <div className="rounded-xl border border-border bg-surface p-5 shadow-sm">
            <div className="flex size-10 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700">
              <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
              </svg>
            </div>
            <h4 className="mt-3 text-sm font-bold text-foreground">Dönüşüm Hunisi</h4>
            <p className="mt-1 text-xs leading-relaxed text-muted">
              Ziyaretçilerin hangi aşamalarda sepete ürün eklediğini veya ödeme adımında ayrıldığını görsel hunilerle analiz edin.
            </p>
          </div>

          <div className="rounded-xl border border-border bg-surface p-5 shadow-sm">
            <div className="flex size-10 items-center justify-center rounded-lg bg-indigo-50 text-indigo-700">
              <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M15 15l-2 5L9 9l11 4-5 2zm0 0l5 5M7.188 2.239l.777 2.897M5.136 7.965l-2.898-.777M13.95 4.05l-2.122 2.122m-5.657 5.656l-2.12 2.122" />
              </svg>
            </div>
            <h4 className="mt-3 text-sm font-bold text-foreground">UTM & Trafik Kaynakları</h4>
            <p className="mt-1 text-xs leading-relaxed text-muted">
              Google Ads, Meta Ads ve organik aramalardan gelen siparişlerin gelir katkısını ve ROAS performansını takip edin.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
