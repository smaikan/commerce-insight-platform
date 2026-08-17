import type { Metadata } from "next";

import { siteConfig } from "@/lib/site-config";
import { safeStoreSettingsUrl } from "@/modules/store-settings/url";

type RootMetadataSettings = {
  displayName?: string | null;
  faviconUrl?: string | null;
};

// Burada root metadata sözleşmesini tek yerde kurup public mağaza adını sekme başlığına, güvenli favicon'u belge başlığına ekliyorum.
export function buildRootMetadata(settings: RootMetadataSettings | null | undefined): Metadata {
  const storeName = settings?.displayName?.trim() || siteConfig.name;
  const safeFaviconUrl = safeStoreSettingsUrl(settings?.faviconUrl);

  return {
    metadataBase: new URL(siteConfig.url),
    title: {
      default: storeName,
      template: `%s | ${storeName}`,
    },
    description: siteConfig.description,
    icons: safeFaviconUrl ? { icon: safeFaviconUrl } : undefined,
    openGraph: {
      type: "website",
      locale: "tr_TR",
      siteName: storeName,
      title: storeName,
      description: siteConfig.description,
      url: "/",
    },
  };
}
