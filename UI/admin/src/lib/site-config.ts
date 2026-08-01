function absoluteOrigin(value: string | undefined, fallback: string): string {
  const candidate = value?.trim() || fallback;
  return new URL(candidate).origin;
}

export const siteConfig = {
  name: process.env.SITE_NAME?.trim() || "E-Commerce",
  url: absoluteOrigin(process.env.SITE_URL, "http://localhost:3000"),
  apiUrl: absoluteOrigin(process.env.API_BASE_URL, "http://localhost:5132"),
  currency: process.env.STORE_CURRENCY?.trim().toUpperCase() || "TRY",
} as const;
