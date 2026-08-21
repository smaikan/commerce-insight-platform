import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";

export const metadata: Metadata = {
  title: "Meta Reklam Yönetimi | Pazarlama",
  description: "Facebook & Instagram reklam yönetimi, Meta Piksel ve Conversions API entegrasyonu.",
};

export default async function MetaAdsPage() {
  await requireAdminPageSession("/marketing/meta-ads");

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title="Meta Reklam Yönetimi"
        description="Facebook ve Instagram reklamlarınızı, Meta Piksel (Pixel), Conversions API (CAPI) ve dinamik ürün kataloglarınızı tek merkezden yönetin."
      />

      {/* Geliştirme Aşamasında Bilgilendirme Kartı */}
      <div className="rounded-2xl border border-amber-200 bg-amber-50/70 p-6 text-amber-950 shadow-sm sm:p-8">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-start gap-4">
            <div className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-amber-500 text-white shadow-sm">
              <svg className="size-6" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2C6.477 2 2 6.477 2 12c0 4.991 3.657 9.128 8.438 9.879V14.89h-2.54V12h2.54V9.797c0-2.506 1.492-3.89 3.777-3.89 1.094 0 2.238.195 2.238.195v2.46h-1.26c-1.243 0-1.63.771-1.63 1.562V12h2.773l-.443 2.89h-2.33v6.989C18.343 21.129 22 16.99 22 12c0-5.523-4.477-10-10-10z" />
              </svg>
            </div>
            <div>
              <div className="inline-flex items-center gap-2 rounded-full border border-amber-300 bg-amber-100/80 px-2.5 py-0.5 text-xs font-bold text-amber-900">
                <span className="size-2 rounded-full bg-amber-600 animate-pulse" />
                Geliştirme Aşamasında
              </div>
              <h2 className="mt-2 text-xl font-bold tracking-tight text-amber-950 sm:text-2xl">
                Meta Reklam & Piksel Entegrasyonu Çok Yakında
              </h2>
              <p className="mt-1.5 text-sm leading-relaxed text-amber-900/80 max-w-3xl">
                Bu modül üzerinden Facebook & Instagram Meta Piksel (Pixel) ve Conversions API (CAPI) erişim anahtarlarınızı tek tıkla tanımlayabilecek; dinamik ürün kataloglarınızı otomatik besleyerek reklam harcamalarınızın getirisini (ROAS) doğrudan takip edebileceksiniz.
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
        <h3 className="text-base font-bold text-foreground">Planlanan Özellikler ve Entegrasyonlar</h3>
        <p className="mt-1 text-xs text-muted">
          Meta reklam modülü aktif olduğunda kullanıma sunulacak temel yetenekler.
        </p>

        <div className="mt-4 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          <div className="rounded-xl border border-border bg-surface p-5 shadow-sm">
            <div className="flex size-10 items-center justify-center rounded-lg bg-blue-50 text-blue-700">
              <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
            <h4 className="mt-3 text-sm font-bold text-foreground">Meta Pixel & CAPI</h4>
            <p className="mt-1 text-xs leading-relaxed text-muted">
              Sunucu taraflı Conversions API ile iOS 14+ engellerini aşan, %100 kayıpsız sepet ve satın alma olay iletimi.
            </p>
          </div>

          <div className="rounded-xl border border-border bg-surface p-5 shadow-sm">
            <div className="flex size-10 items-center justify-center rounded-lg bg-pink-50 text-pink-700">
              <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
              </svg>
            </div>
            <h4 className="mt-3 text-sm font-bold text-foreground">Dinamik Ürün Kataloğu</h4>
            <p className="mt-1 text-xs leading-relaxed text-muted">
              Instagram Shop ve Facebook Marketplace için gerçek zamanlı stok ve fiyat senkronizasyonlu katalog akışı.
            </p>
          </div>

          <div className="rounded-xl border border-border bg-surface p-5 shadow-sm">
            <div className="flex size-10 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700">
              <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <h4 className="mt-3 text-sm font-bold text-foreground">ROAS & Harcama Takibi</h4>
            <p className="mt-1 text-xs leading-relaxed text-muted">
              Reklam kampanyalarınızın getirdiği ciro, edinme maliyeti (CPA) ve net reklam kârlılık oranları.
            </p>
          </div>

          <div className="rounded-xl border border-border bg-surface p-5 shadow-sm">
            <div className="flex size-10 items-center justify-center rounded-lg bg-purple-50 text-purple-700">
              <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <h4 className="mt-3 text-sm font-bold text-foreground">Yeniden Pazarlama (Retargeting)</h4>
            <p className="mt-1 text-xs leading-relaxed text-muted">
              Sepetini terk eden veya ürün inceleyen kullanıcılara özel dinamik kitle ve reklam hedefleme listeleri.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
