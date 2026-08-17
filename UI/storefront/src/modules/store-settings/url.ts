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
