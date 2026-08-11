import "server-only";

import { siteConfig } from "@/lib/site-config";

// Burada cookie tabanlı mutation isteklerinin yalnızca yapılandırılmış Storefront origin'inden geldiğini doğruluyorum.
export function hasTrustedStorefrontOrigin(request: Request): boolean {
  const origin = request.headers.get("origin");
  return Boolean(origin && origin === new URL(siteConfig.url).origin);
}
