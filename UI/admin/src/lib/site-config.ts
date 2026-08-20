function absoluteOrigin(value: string | undefined, fallback: string): string {
  const candidate = value?.trim() || fallback;
  return new URL(candidate).origin;
}

export const siteConfig = {
  name: process.env.SITE_NAME?.trim() || "Mağaza",
  url: absoluteOrigin(process.env.ADMIN_APP_ORIGIN || process.env.SITE_URL, "http://localhost:3001"),
  currency: process.env.STORE_CURRENCY?.trim().toUpperCase() || "TRY",
} as const;
