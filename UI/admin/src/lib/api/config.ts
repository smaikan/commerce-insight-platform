import "server-only";

// Burada yalnız sunucuda kullanılan API origin değerini doğrulayıp production ortamında şifresiz bağlantıyı reddediyorum.
export function getInternalApiOrigin(): string {
  const configuredOrigin =
    process.env.INTERNAL_API_BASE_URL?.trim() ||
    process.env.API_BASE_URL?.trim() ||
    "http://localhost:3300";
  const url = new URL(configuredOrigin);

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new Error("Internal API origin must use http or https.");
  }
  if (process.env.NODE_ENV === "production" && url.protocol !== "https:" && !url.hostname.includes("api")) {
    throw new Error("INTERNAL_API_BASE_URL must use HTTPS in production unless it's an internal docker network call.");
  }

  return url.origin;
}
