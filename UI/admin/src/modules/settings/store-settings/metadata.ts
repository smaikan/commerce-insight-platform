import type { Metadata } from "next";
import { siteConfig } from "@/lib/site-config";

// Burada admin metadata'sını sabit noindex kurallarıyla ve yalnız güvenli StoreSettings favicon adresiyle oluşturuyorum.
export function buildAdminRootMetadata(faviconUrl: string | null | undefined): Metadata {
  const safeFaviconUrl = safeHttpUrl(faviconUrl);

  return {
    metadataBase: new URL(siteConfig.url),
    title: {
      default: `Yönetim Paneli | ${siteConfig.name}`,
      template: `%s | ${siteConfig.name} Yönetim Paneli`,
    },
    description: `${siteConfig.name} operasyon yönetim paneli.`,
    robots: {
      index: false,
      follow: false,
      nocache: true,
    },
    icons: safeFaviconUrl ? { icon: safeFaviconUrl } : undefined,
  };
}

// Burada API'den gelen favicon değerini belge başlığına yalnız HTTP veya HTTPS adresiyse taşıyorum.
function safeHttpUrl(value: string | null | undefined): string | null {
  if (!value) return null;

  try {
    const url = new URL(value);
    return url.protocol === "https:" || url.protocol === "http:" ? url.toString() : null;
  } catch {
    return null;
  }
}
