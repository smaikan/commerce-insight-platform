import type { Metadata } from "next";
import Link from "next/link";

import { ContactForm } from "@/modules/contact/components/contact-form";
import { getPublicStoreSettings } from "@/modules/store-settings/api";
import { safeStoreSettingsUrl } from "@/modules/store-settings/url";
import { siteConfig } from "@/lib/site-config";

export async function generateMetadata(): Promise<Metadata> {
  const settings = await getPublicStoreSettings().catch(() => null);
  const displayName = settings?.displayName?.trim() || siteConfig.name;

  return {
    title: `İletişim | ${displayName}`,
    description: `${displayName} müşteri hizmetleri, sipariş desteği, mağaza adresi ve iletişim kanalları. Sorularınız için bizimle kolayca iletişime geçin.`,
    alternates: {
      canonical: "/contact",
    },
    openGraph: {
      title: `İletişim | ${displayName}`,
      description: `${displayName} müşteri hizmetleri ve iletişim bilgileri.`,
      url: "/contact",
    },
  };
}

function formatPhoneDisplay(phone: string): string {
  const clean = phone.replace(/[^0-9+]/g, "");
  if (clean.length === 11 && clean.startsWith("0")) {
    return `${clean.slice(0, 4)} ${clean.slice(4, 7)} ${clean.slice(7, 9)} ${clean.slice(9, 11)}`;
  }
  if (clean.length === 10) {
    return `0${clean.slice(0, 3)} ${clean.slice(3, 6)} ${clean.slice(6, 8)} ${clean.slice(8, 10)}`;
  }
  return phone;
}

function getWhatsappLink(phone: string): string {
  const digits = phone.replace(/[^0-9]/g, "");
  if (digits.startsWith("90")) return `https://wa.me/${digits}`;
  if (digits.startsWith("0")) return `https://wa.me/90${digits.slice(1)}`;
  return `https://wa.me/90${digits}`;
}

export default async function ContactPage() {
  const settings = await getPublicStoreSettings().catch(() => null);
  const turnstileSiteKey = process.env.TURNSTILE_SITE_KEY?.trim() || "";
  const turnstileRequired = process.env.NODE_ENV === "production";

  const displayName = settings?.displayName?.trim() || siteConfig.name;
  const supportPhone = settings?.supportPhone?.trim() || "0536 256 78 45";
  const supportEmail = settings?.supportEmail?.trim() || "info@eleven.com";
  const whatsappNumber = settings?.whatsappNumber?.trim() || "0549 586 23 45";
  const contactAddress = settings?.contactAddress?.trim() || "Altıyol Meydanı, Söğütlüçeşme Cad., 34714 Kadıköy/İstanbul";
  const workingHours = settings?.workingHours?.trim() || "Pazartesi - Cumartesi: 09:00 - 18:00";
  const mapUrl = settings?.mapUrl ? safeStoreSettingsUrl(settings.mapUrl) : null;
  const isEmbedMap = mapUrl?.includes("google.com/maps/embed") ?? false;
  const mapsSearchUrl = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(contactAddress)}`;
  const whatsappHref = getWhatsappLink(whatsappNumber);

  return (
    <main id="main-content" className="flex-1 pb-16 sm:pb-24">
      {/* Hero Header */}
      <header className="border-b border-line bg-surface-subtle/30 py-10 sm:py-14">
        <div className="page-shell">
          <nav aria-label="Breadcrumb" className="mb-4 text-xs font-medium text-ink-muted">
            <ol className="flex items-center gap-2">
              <li>
                <Link href="/" className="hover:text-brand-950 transition-colors">Ana Sayfa</Link>
              </li>
              <li aria-hidden="true">/</li>
              <li className="text-ink font-semibold" aria-current="page">İletişim</li>
            </ol>
          </nav>
          <div className="max-w-2xl">
            <span className="inline-block text-xs font-bold uppercase tracking-widest text-brand-700">
              Müşteri Deneyimi & Destek
            </span>
            <h1 className="mt-2 text-3xl font-bold tracking-tight text-brand-950 sm:text-4xl">
              Size Yardımcı Olmaktan Mutluluk Duyarız
            </h1>
            <p className="mt-3 text-sm leading-relaxed text-ink-muted sm:text-base">
              Siparişleriniz, ürünlerimiz, özel talepleriniz veya önerileriniz için aşağıdaki kanallardan bize ulaşabilir veya doğrudan mesaj formu doldurabilirsiniz.
            </p>
          </div>
        </div>
      </header>

      <div className="page-shell mt-10 sm:mt-14">
        {/* Hızlı İletişim Kartları */}
        <section aria-label="Hızlı iletişim kanalları" className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {/* Telefon Kartı */}
          <div className="flex flex-col justify-between rounded-2xl border border-line bg-surface p-6 shadow-sm transition-all hover:border-brand-600">
            <div>
              <div className="flex size-11 items-center justify-center rounded-xl bg-brand-50 text-brand-700">
                <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                </svg>
              </div>
              <h3 className="mt-4 text-base font-bold text-ink">Telefon Desteği</h3>
              <p className="mt-1 text-xs text-ink-muted leading-relaxed">
                Hafta içi ve cumartesi günleri müşteri temsilcimizle görüşebilirsiniz.
              </p>
            </div>
            <div className="mt-6 pt-4 border-t border-line/60">
              <a
                href={`tel:${supportPhone.replace(/[^0-9+]/g, "")}`}
                className="focus-ring inline-flex items-center text-sm font-bold text-brand-700 hover:text-brand-950 transition-colors"
              >
                {formatPhoneDisplay(supportPhone)}
              </a>
            </div>
          </div>

          {/* E-posta Kartı */}
          <div className="flex flex-col justify-between rounded-2xl border border-line bg-surface p-6 shadow-sm transition-all hover:border-brand-600">
            <div>
              <div className="flex size-11 items-center justify-center rounded-xl bg-brand-50 text-brand-700">
                <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </div>
              <h3 className="mt-4 text-base font-bold text-ink">E-posta Danışma</h3>
              <p className="mt-1 text-xs text-ink-muted leading-relaxed">
                Tüm sorularınız ve kurumsal talepleriniz mümkün olan en kısa sürede yanıtlanır.
              </p>
            </div>
            <div className="mt-6 pt-4 border-t border-line/60">
              <a
                href={`mailto:${supportEmail}`}
                className="focus-ring inline-flex items-center text-sm font-bold text-brand-700 hover:text-brand-950 transition-colors break-all"
              >
                {supportEmail}
              </a>
            </div>
          </div>

          {/* WhatsApp Kartı */}
          <div className="flex flex-col justify-between rounded-2xl border border-line bg-surface p-6 shadow-sm transition-all hover:border-emerald-600">
            <div>
              <div className="flex size-11 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                <svg className="size-5" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z" />
                </svg>
              </div>
              <h3 className="mt-4 text-base font-bold text-ink">WhatsApp Canlı Destek</h3>
              <p className="mt-1 text-xs text-ink-muted leading-relaxed">
                Hızlı soru sorma ve anlık sipariş danışmanlığı için bize yazın.
              </p>
            </div>
            <div className="mt-6 pt-4 border-t border-line/60">
              <a
                href={whatsappHref}
                target="_blank"
                rel="noreferrer"
                className="focus-ring inline-flex items-center text-sm font-bold text-emerald-700 hover:text-emerald-900 transition-colors"
              >
                Sohbeti Başlat &rarr;
              </a>
            </div>
          </div>

          {/* Adres Kartı */}
          <div className="flex flex-col justify-between rounded-2xl border border-line bg-surface p-6 shadow-sm transition-all hover:border-brand-600">
            <div>
              <div className="flex size-11 items-center justify-center rounded-xl bg-brand-50 text-brand-700">
                <svg className="size-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </div>
              <h3 className="mt-4 text-base font-bold text-ink">Merkez & Showroom</h3>
              <p className="mt-1 text-xs text-ink-muted leading-relaxed line-clamp-2">
                {contactAddress}
              </p>
            </div>
            <div className="mt-6 pt-4 border-t border-line/60">
              <a
                href={mapsSearchUrl}
                target="_blank"
                rel="noreferrer"
                className="focus-ring inline-flex items-center text-sm font-bold text-brand-700 hover:text-brand-950 transition-colors"
              >
                Haritada Yol Tarifi Al &rarr;
              </a>
            </div>
          </div>
        </section>

        {/* Ana İçerik: İletişim Formu + Sıkça Sorulan Sorular / Bilgiler */}
        <div className="mt-12 grid gap-12 lg:grid-cols-12 lg:items-start sm:mt-16">
          {/* Sol Kolon: Form */}
          <div className="lg:col-span-7">
            <div className="mb-6">
              <h2 className="text-2xl font-bold tracking-tight text-brand-950">Bize Mesaj Gönderin</h2>
              <p className="mt-1 text-sm text-ink-muted">
                Talebiniz güvenli biçimde müşteri deneyimi ekibimize iletilir. Gönderim sonunda verilen referans numarasıyla kaydınızı takip edebilirsiniz.
              </p>
            </div>
            <ContactForm turnstileSiteKey={turnstileSiteKey} turnstileRequired={turnstileRequired} />
          </div>

          {/* Sağ Kolon: Sıkça Sorulanlar & Mağaza Bilgileri */}
          <div className="space-y-6 lg:col-span-5">
            {/* Çalışma Saatleri Kartı */}
            <div className="rounded-2xl border border-line bg-surface p-6 shadow-sm">
              <h3 className="flex items-center gap-2 text-base font-bold text-brand-950">
                <svg className="size-5 text-brand-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                Çalışma Saatlerimiz
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-ink-muted">
                {workingHours}
              </p>
              <p className="mt-1 text-xs text-ink-muted">
                Pazar günleri ve resmi tatillerde web sitemiz üzerinden 7/24 sipariş verebilirsiniz.
              </p>
            </div>

            {/* Hızlı SSS Kartları */}
            <div className="rounded-2xl border border-line bg-surface p-6 shadow-sm">
              <h3 className="text-base font-bold text-brand-950">Sıkça Sorulan Sorular</h3>
              <div className="mt-4 space-y-4 text-sm divide-y divide-line/60">
                <div className="pt-3 first:pt-0">
                  <h4 className="font-semibold text-ink">Siparişimin durumunu nasıl takip edebilirim?</h4>
                  <p className="mt-1 text-xs leading-relaxed text-ink-muted">
                    Sipariş durumunuzu <Link href="/account/orders" className="text-brand-700 underline hover:text-brand-950">Siparişlerim</Link> sayfasından veya kargo takip numaranızla anlık olarak izleyebilirsiniz.
                  </p>
                </div>
                <div className="pt-3">
                  <h4 className="font-semibold text-ink">Kargo ve teslimat süresi ne kadardır?</h4>
                  <p className="mt-1 text-xs leading-relaxed text-ink-muted">
                    Siparişleriniz 1-3 iş günü içinde özenle hazırlanıp anlaşmalı kargo firmasına teslim edilir.
                  </p>
                </div>
                <div className="pt-3">
                  <h4 className="font-semibold text-ink">İade ve değişim şartları nelerdir?</h4>
                  <p className="mt-1 text-xs leading-relaxed text-ink-muted">
                    14 gün içerisinde kullanılmamış ve orijinal ambalajı bozulmamış ürünlerde koşulsuz iade hakkınız bulunmaktadır. Detaylar için <Link href="/cancellation-and-refund" className="text-brand-700 underline hover:text-brand-950">İptal ve İade</Link> sayfamızı inceleyebilirsiniz.
                  </p>
                </div>
              </div>
            </div>

            {/* Güvence Rozeti */}
            <div className="rounded-2xl border border-brand-200 bg-brand-50/60 p-5 text-xs leading-relaxed text-brand-950">
              <div className="flex items-center gap-2 font-bold text-brand-900">
                <svg className="size-4 text-brand-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                </svg>
                %100 Güvenli Alışveriş & Orijinal Ürün Garantisi
              </div>
              <p className="mt-1 text-ink-muted">
                {displayName} üzerinden gerçekleştirdiğiniz tüm alışverişler 256-bit SSL güvenlik sertifikası ve yetkili ödeme altyapısıyla korunmaktadır.
              </p>
            </div>
          </div>
        </div>

        {/* Harita Bölümü */}
        {isEmbedMap && mapUrl ? (
          <section aria-label="Harita konumu" className="mt-16">
            <div className="overflow-hidden rounded-2xl border border-line bg-surface shadow-sm">
              <div className="border-b border-line bg-surface-subtle/50 px-6 py-4 flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h3 className="text-base font-bold text-brand-950">Harita Konumu</h3>
                  <p className="text-xs text-ink-muted">{contactAddress}</p>
                </div>
                <a
                  href={mapsSearchUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="focus-ring inline-flex items-center text-xs font-bold text-brand-700 hover:text-brand-950"
                >
                  Google Haritalarda Aç ↗
                </a>
              </div>
              <div className="h-80 sm:h-96 w-full">
                <iframe
                  title="Mağaza Konumu"
                  src={mapUrl}
                  width="100%"
                  height="100%"
                  style={{ border: 0 }}
                  allowFullScreen
                  loading="lazy"
                  referrerPolicy="no-referrer-when-downgrade"
                  className="w-full h-full"
                />
              </div>
            </div>
          </section>
        ) : null}
      </div>
    </main>
  );
}
