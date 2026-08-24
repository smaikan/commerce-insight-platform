// Burada mağaza ayarlarındaki bağlantıları yalnızca güvenli HTTP/HTTPS adresleri olarak ortaklaştırıyorum.
export function safeStoreSettingsUrl(value: string | null | undefined): string | null {
  if (!value) return null;
  try {
    const url = new URL(value);
    return url.protocol === "https:" || url.protocol === "http:" ? url.toString() : null;
  } catch {
    return null;
  }
}

// Google'ın Embed API adresi normal sekmede açılamaz; bu ayrım embed adresinin yalnız iframe src olarak kullanılmasını sağlar.
export function safeGoogleMapsEmbedUrl(value: string | null | undefined): string | null {
  const safeUrl = safeStoreSettingsUrl(value);
  if (!safeUrl) return null;

  const url = new URL(safeUrl);
  const hostname = url.hostname.toLowerCase();
  const isGoogleHost = hostname === "google.com" || hostname === "www.google.com" || hostname === "maps.google.com";
  const isEmbedPath = url.pathname === "/maps/embed" || url.pathname.startsWith("/maps/embed/");

  return url.protocol === "https:" && isGoogleHost && isEmbedPath ? url.toString() : null;
}

// Embed URL'sini üst pencereye yönlendirmek yerine adresi Google Maps'in normal arama görünümünde açıyorum.
export function storeMapNavigationUrl(
  mapUrl: string | null | undefined,
  address: string | null | undefined,
): string | null {
  const safeMapUrl = safeStoreSettingsUrl(mapUrl);
  if (!safeMapUrl) return null;
  if (!safeGoogleMapsEmbedUrl(safeMapUrl)) return safeMapUrl;

  const query = address?.trim();
  if (!query) return null;

  const url = new URL("https://www.google.com/maps/search/");
  url.searchParams.set("api", "1");
  url.searchParams.set("query", query);
  return url.toString();
}
