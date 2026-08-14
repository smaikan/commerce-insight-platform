import "server-only";

const CANONICAL_TOKEN_PATTERN = /^[0-9A-F]{64}$/;

// Burada ortak guest session cookie'sinin canonical değerini yalnızca sunucu tarafındaki BFF akışlarına açıyorum.
export function guestCookieToken(cookieHeader: string | null, name: string): string | null {
  const forwarded = guestCookieHeader(cookieHeader, [name]);
  return forwarded?.slice(name.length + 1) || null;
}

// Burada browser cookie başlığından yalnız izin verilen canonical guest token'larını upstream isteğine taşıyorum.
export function guestCookieHeader(cookieHeader: string | null, allowedNames: readonly string[]): string | undefined {
  if (!cookieHeader) return undefined;

  const cookies = new Map(
    cookieHeader.split(";").map((part) => {
      const separator = part.indexOf("=");
      return separator < 0
        ? [part.trim(), ""]
        : [part.slice(0, separator).trim(), part.slice(separator + 1).trim()];
    }),
  );

  const forwarded = allowedNames.flatMap((name) => {
    const value = cookies.get(name);
    return value && CANONICAL_TOKEN_PATTERN.test(value) ? [`${name}=${value}`] : [];
  });

  return forwarded.length ? forwarded.join("; ") : undefined;
}

// Burada upstream Set-Cookie değerlerini allowlist ve canonical token kontrolünden geçirip güvenli Storefront nitelikleriyle yeniden yazıyorum.
export function appendAllowedGuestSetCookies(
  source: Headers,
  target: Headers,
  allowedNames: readonly string[],
): void {
  const getSetCookie = (source as Headers & { getSetCookie?: () => string[] }).getSetCookie;
  const values = getSetCookie
    ? getSetCookie.call(source)
    : [source.get("set-cookie")].filter((value): value is string => Boolean(value));

  for (const value of values) {
    const sanitized = sanitizeSetCookie(value, allowedNames);
    if (sanitized) target.append("Set-Cookie", sanitized);
  }
}

// Burada upstream'in ürettiği ortak guest tokenını sonraki aynı BFF isteğinde kullanmak için güvenli biçimde ayıklıyorum.
export function guestTokenFromSetCookie(source: Headers, name: string): string | null {
  const getSetCookie = (source as Headers & { getSetCookie?: () => string[] }).getSetCookie;
  const values = getSetCookie
    ? getSetCookie.call(source)
    : [source.get("set-cookie")].filter((value): value is string => Boolean(value));

  for (const value of values) {
    const firstPart = value.split(";", 1)[0] || "";
    const separator = firstPart.indexOf("=");
    if (separator <= 0 || firstPart.slice(0, separator) !== name) continue;
    const token = firstPart.slice(separator + 1);
    if (CANONICAL_TOKEN_PATTERN.test(token)) return token;
  }

  return null;
}

function sanitizeSetCookie(value: string, allowedNames: readonly string[]): string | null {
  const parts = value.split(";").map((part) => part.trim());
  const separator = parts[0]?.indexOf("=") ?? -1;
  if (separator <= 0) return null;

  const name = parts[0].slice(0, separator);
  const token = parts[0].slice(separator + 1);
  if (!allowedNames.includes(name)) return null;

  const attributes = new Map<string, string | true>();
  for (const part of parts.slice(1)) {
    const attributeSeparator = part.indexOf("=");
    const key = (attributeSeparator < 0 ? part : part.slice(0, attributeSeparator)).trim().toLowerCase();
    const attributeValue = attributeSeparator < 0 ? true : part.slice(attributeSeparator + 1).trim();
    if (key) attributes.set(key, attributeValue);
  }

  const isDeletion = token === "" && (attributes.has("expires") || attributes.get("max-age") === "0");
  if (!isDeletion && !CANONICAL_TOKEN_PATTERN.test(token)) return null;

  const output = [`${name}=${token}`, "Path=/", "HttpOnly", "SameSite=Lax"];
  const maxAge = attributes.get("max-age");
  const expires = attributes.get("expires");

  if (typeof maxAge === "string" && /^-?\d+$/.test(maxAge)) output.push(`Max-Age=${maxAge}`);
  if (typeof expires === "string" && !Number.isNaN(Date.parse(expires))) output.push(`Expires=${expires}`);
  if (process.env.NODE_ENV === "production") output.push("Secure");

  return output.join("; ");
}
