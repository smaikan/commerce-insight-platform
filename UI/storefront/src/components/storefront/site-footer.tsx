import Image from "next/image";
import Link from "next/link";

import type { StorefrontNavigationItem } from "@/components/storefront/navigation-types";
import { siteConfig } from "@/lib/site-config";
import { getStorefrontNavigation } from "@/modules/catalog/navigation";
import { legalLinks } from "@/modules/legal/legal-links";
import { getPublicStoreSettings } from "@/modules/store-settings/api";
import type { PublicStoreSettings } from "@/modules/store-settings/types";

type FooterSettings = Pick<
  PublicStoreSettings,
  | "displayName"
  | "shortDescription"
  | "logoUrl"
  | "supportEmail"
  | "supportPhone"
  | "whatsappNumber"
  | "contactAddress"
  | "workingHours"
  | "mapUrl"
  | "facebookUrl"
  | "instagramUrl"
  | "tiktokUrl"
  | "youtubeUrl"
  | "xUrl"
  | "pinterestUrl"
>;

type SocialKey = "facebookUrl" | "instagramUrl" | "tiktokUrl" | "youtubeUrl" | "xUrl" | "pinterestUrl";

// Burada API erişilemezse ortak footer'ın sayfayı çökertmeden yalnız doğrulanmış yerel marka bilgisiyle kalmasını sağlıyorum.
const FALLBACK_FOOTER_SETTINGS: FooterSettings = {
  displayName: siteConfig.name,
  shortDescription: siteConfig.description,
  logoUrl: null,
  supportEmail: null,
  supportPhone: null,
  whatsappNumber: null,
  contactAddress: null,
  workingHours: null,
  mapUrl: null,
  facebookUrl: null,
  instagramUrl: null,
  tiktokUrl: null,
  youtubeUrl: null,
  xUrl: null,
  pinterestUrl: null,
};

// Burada public ayarlardaki sosyal hesapları referanstaki kompakt dairesel sıraya dönüştürüyorum.
const SOCIAL_LINKS: Array<{ key: SocialKey; label: string }> = [
  { key: "facebookUrl", label: "Facebook" },
  { key: "instagramUrl", label: "Instagram" },
  { key: "youtubeUrl", label: "YouTube" },
  { key: "tiktokUrl", label: "TikTok" },
  { key: "xUrl", label: "X" },
  { key: "pinterestUrl", label: "Pinterest" },
];

// Burada public kimlik/iletişim ayarlarını ve gerçek kategori hedeflerini paralel okuyarak footer görünümüne aktarıyorum.
export async function SiteFooter() {
  const [settingsResult, navigationResult] = await Promise.allSettled([
    getPublicStoreSettings(),
    getStorefrontNavigation(),
  ]);
  const settings = settingsResult.status === "fulfilled" ? settingsResult.value : FALLBACK_FOOTER_SETTINGS;
  const categories = navigationResult.status === "fulfilled"
    ? navigationResult.value.find((group) => group.id === "categories")?.items.slice(0, 6) || []
    : [];

  return <SiteFooterView settings={settings} categories={categories} />;
}

// Burada referanstaki marka, iletişim, kategori ve müşteri hizmetleri kolonlarını responsive ve semantik footer olarak kuruyorum.
export function SiteFooterView({ settings, categories }: { settings: FooterSettings; categories: StorefrontNavigationItem[] }) {
  const displayName = settings.displayName.trim() || siteConfig.name;
  const logoUrl = safeHttpUrl(settings.logoUrl);
  const mapUrl = safeHttpUrl(settings.mapUrl);
  const socialLinks = SOCIAL_LINKS.flatMap((social) => {
    const href = safeHttpUrl(settings[social.key]);
    return href ? [{ ...social, href }] : [];
  });
  const whatsappHref = whatsappUrl(settings.whatsappNumber);
  const hasContact = Boolean(settings.contactAddress || settings.supportPhone || settings.supportEmail || settings.workingHours);

  return (
    <footer className="mt-auto border-t-4 border-brand-600 bg-footer text-footer-ink">
      {/* Burada footer kolonlarını ana sayfa genişliğiyle hizalayıp daha sakin ve kompakt bir bilgi ritmi kuruyorum. */}
      <div className="page-shell grid gap-8 pt-9 pb-7 sm:grid-cols-2 sm:gap-x-10 sm:gap-y-9 sm:pt-10 sm:pb-8 lg:grid-cols-[minmax(14rem,1.2fr)_minmax(14rem,1fr)_minmax(9rem,0.65fr)_minmax(14rem,1fr)] lg:gap-x-9 lg:pt-11 lg:pb-8 xl:gap-x-12">
        <section aria-label="Mağaza bilgileri">
          <Link href="/" prefetch={false} className="focus-ring inline-flex max-w-full items-center" aria-label={`${displayName} ana sayfa`}>
            {logoUrl ? (
              <Image src={logoUrl} alt={displayName} width={260} height={84} className="h-auto max-h-20 w-auto max-w-full object-contain object-left" />
            ) : (
              <span className="text-xl font-black tracking-[0.13em] sm:text-2xl">{displayName}</span>
            )}
          </Link>
          {settings.shortDescription ? (
            <p className="mt-3 max-w-sm text-sm leading-6 text-footer-muted">{settings.shortDescription}</p>
          ) : null}
          {socialLinks.length > 0 ? (
            <nav className="mt-4 flex flex-wrap gap-2" aria-label="Sosyal medya hesapları">
              {socialLinks.map((social) => (
                <a
                  key={social.key}
                  href={social.href}
                  target="_blank"
                  rel="noreferrer"
                  className="focus-ring inline-flex size-10 items-center justify-center rounded-lg border border-footer-line bg-footer-chip text-footer-ink transition-colors hover:border-brand-600 hover:bg-brand-700"
                  aria-label={`${social.label} hesabımızı aç`}
                >
                  <SocialIcon name={social.label} />
                </a>
              ))}
            </nav>
          ) : null}
        </section>

        <section id="store-contact" className="scroll-mt-28" aria-labelledby="footer-contact-title">
          <h2 id="footer-contact-title" className="text-sm font-bold tracking-[0.08em] uppercase">İletişim</h2>
          <address className="mt-4 flex flex-col items-start gap-2 not-italic text-sm leading-6 text-footer-muted">
            {settings.contactAddress ? (
              mapUrl ? <a className="footer-link whitespace-pre-line" href={mapUrl} target="_blank" rel="noreferrer">{settings.contactAddress}</a> : <span className="whitespace-pre-line">{settings.contactAddress}</span>
            ) : null}
            {settings.supportPhone ? <a className="footer-link" href={`tel:${phoneHref(settings.supportPhone)}`}>{settings.supportPhone}</a> : null}
            {settings.supportEmail ? <a className="footer-link break-all" href={`mailto:${settings.supportEmail}`}>{settings.supportEmail}</a> : null}
            {settings.workingHours ? <span className="whitespace-pre-line">{settings.workingHours}</span> : null}
            {!hasContact ? <p className="max-w-sm">Mağaza iletişim bilgileri yayınlandığında burada görüntülenecektir.</p> : null}
          </address>
        </section>

        <section aria-labelledby="footer-categories-title">
          <h2 id="footer-categories-title" className="text-sm font-bold tracking-[0.08em] uppercase">Kategoriler</h2>
          <nav className="mt-4 flex flex-col items-start" aria-label="Footer kategorileri">
            {categories.length > 0 ? categories.map((category) => (
              <Link key={category.id} className="footer-link inline-flex min-h-10 items-center text-sm" href={category.href} prefetch={false}>{category.label}</Link>
            )) : (
              <Link className="footer-link inline-flex min-h-10 items-center text-sm" href="/products" prefetch={false}>Tüm ürünler</Link>
            )}
          </nav>
        </section>

        <section aria-labelledby="footer-customer-title">
          <h2 id="footer-customer-title" className="text-sm font-bold tracking-[0.08em] uppercase">Müşteri Hizmetleri</h2>
          <nav className="mt-4 flex flex-col items-start" aria-label="Yasal ve müşteri bilgilendirme bağlantıları">
            {legalLinks.map((link) => (
              <Link key={link.href} className="footer-link inline-flex min-h-10 items-center text-sm" href={link.href} prefetch={false}>{link.label}</Link>
            ))}
            <a className="footer-link inline-flex min-h-10 items-center text-sm" href="#store-contact">İletişim</a>
          </nav>
        </section>
      </div>

      <div className="border-t border-footer-line">
        {/* Burada yasal alt satırın özellikle alttaki gereksiz yüksekliğini azaltıp metinlere nefes payı bırakıyorum. */}
        <div className="page-shell flex flex-col gap-1.5 py-3.5 text-xs text-footer-muted sm:flex-row sm:items-center sm:justify-between">
          <p>© {new Date().getFullYear()} {displayName}</p>
          <p>TR · {siteConfig.currency}</p>
        </div>
      </div>

      {whatsappHref ? (
        <a
          href={whatsappHref}
          target="_blank"
          rel="noreferrer"
          className="focus-ring fixed bottom-5 left-5 z-30 inline-flex size-14 items-center justify-center rounded-full bg-whatsapp text-white shadow-floating sm:size-16"
          aria-label="WhatsApp ile iletişime geç"
        >
          <WhatsappIcon />
        </a>
      ) : null}

      <a
        href="#page-top"
        className="focus-ring fixed right-5 bottom-5 z-30 inline-flex size-12 items-center justify-center rounded-full bg-brand-700 text-white shadow-floating transition-colors hover:bg-brand-600 sm:size-14"
        aria-label="Sayfanın başına dön"
      >
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-6" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="m6 15 6-6 6 6" /></svg>
      </a>
    </footer>
  );
}

// Burada yalnız http/https sosyal, logo ve harita adreslerini render ederek güvensiz URL şemalarını eliyorum.
function safeHttpUrl(value: string | null | undefined): string | null {
  if (!value) return null;
  try {
    const url = new URL(value);
    return url.protocol === "https:" || url.protocol === "http:" ? url.toString() : null;
  } catch {
    return null;
  }
}

// Burada telefon değerini tel bağlantısında yalnız arama için anlamlı karakterlerle sınırlıyorum.
function phoneHref(value: string): string {
  return value.replace(/[^+\d]/g, "");
}

// Burada yayınlanan WhatsApp numarasını resmi wa.me hedefinin beklediği rakam biçimine çeviriyorum.
function whatsappUrl(value: string | null | undefined): string | null {
  const digits = value?.replace(/\D/g, "") || "";
  return digits ? `https://wa.me/${digits}` : null;
}

// Burada sosyal hesap türlerini tek renkli, küçük ve yardımcı teknolojiden gizli simgelerle ayırt ediyorum.
function SocialIcon({ name }: { name: string }) {
  if (name === "Instagram") return <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5" fill="none" stroke="currentColor" strokeWidth="1.8"><rect x="3.5" y="3.5" width="17" height="17" rx="5" /><circle cx="12" cy="12" r="4" /><circle cx="17.5" cy="6.7" r="1" fill="currentColor" stroke="none" /></svg>;
  if (name === "YouTube") return <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5" fill="currentColor"><path d="M21 7.3a3 3 0 0 0-2.1-2.1C17 4.7 12 4.7 12 4.7s-5 0-6.9.5A3 3 0 0 0 3 7.3 31 31 0 0 0 2.5 12 31 31 0 0 0 3 16.7a3 3 0 0 0 2.1 2.1c1.9.5 6.9.5 6.9.5s5 0 6.9-.5a3 3 0 0 0 2.1-2.1 31 31 0 0 0 .5-4.7 31 31 0 0 0-.5-4.7ZM10 15.2V8.8l5.5 3.2-5.5 3.2Z" /></svg>;
  if (name === "Facebook") return <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5" fill="currentColor"><path d="M14 8h3V4.5c-.5-.1-2.2-.2-4.1-.2-4 0-6.7 2.4-6.7 6.9V15H2v4h4.2v10h5.1V19h4.1l.7-4h-4.8v-3.4C11.3 9.3 12 8 14 8Z" transform="scale(.72) translate(5 -3)" /></svg>;
  if (name === "TikTok") return <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M15 4v10a5 5 0 1 1-4-4.9" /><path d="M15 4c.7 2.6 2.2 4 5 4" /></svg>;
  if (name === "Pinterest") return <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5" fill="none" stroke="currentColor" strokeWidth="1.8"><circle cx="12" cy="12" r="9" /><path d="M9.5 20 12 9.5m-1.3 5.2c1 1.2 3.8 1.5 5.5-.4 2.2-2.5.9-7-2.8-7.7-4.4-.8-7.2 2.1-6.5 5.2.2.9.8 1.7 1.6 2.1" /></svg>;
  return <svg aria-hidden="true" viewBox="0 0 24 24" className="size-4" fill="currentColor"><path d="M4 3h4.7l4.2 5.7L18 3h2l-6.2 7.2L21 21h-4.7l-4.7-6.4L6 21H4l6.7-7.9L4 3Zm3.7 1.6 9.5 14.8h1.9L9.6 4.6H7.7Z" /></svg>;
}

// Burada referanstaki belirgin WhatsApp eylemini mevcut public ayar varsa erişilebilir tek renkli simgeyle sunuyorum.
function WhatsappIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" className="size-8" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M20 11.6a8 8 0 0 1-11.8 7L4 20l1.4-4A8 8 0 1 1 20 11.6Z" />
      <path d="M8.5 8.2c.5 3.6 3.2 6.2 6.8 6.8l1.1-1.6-2.4-1.1-.8 1c-1.5-.7-2.7-1.9-3.4-3.4l1-.8-1.1-2.4-1.2 1.5Z" />
    </svg>
  );
}
