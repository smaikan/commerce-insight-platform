function absoluteOrigin(value: string | undefined, fallback: string): string {
  const candidate = value?.trim() || fallback;
  return new URL(candidate).origin;
}

// Burada marka, canonical origin, API origin ve para birimini tek sunucu yapılandırmasından okuyorum.
export const siteConfig = {
  name: process.env.SITE_NAME?.trim() || "SERANTIS",
  description:
    process.env.SITE_DESCRIPTION?.trim() ||
    "Özenle seçilen ürünleri keşfedin ve güvenle alışveriş yapın.",
  url: absoluteOrigin(
    process.env.STOREFRONT_APP_ORIGIN || process.env.SITE_URL,
    "http://localhost:3000",
  ),
  apiUrl: absoluteOrigin(
    process.env.INTERNAL_API_BASE_URL || process.env.API_BASE_URL,
    "http://localhost:3300",
  ),
  currency: process.env.STORE_CURRENCY?.trim().toUpperCase() || "TRY",
} as const;
