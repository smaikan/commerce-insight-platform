import { NextRequest, NextResponse } from "next/server";
import { siteConfig } from "@/lib/site-config";
import { ADMIN_ACCESS_COOKIE, ADMIN_REFRESH_COOKIE } from "@/lib/auth/constants";
import { isProtectedAdminPath, safeReturnTo } from "@/lib/auth/policy";

// Burada Proxy'yi yalnız hızlı cookie varlığı kontrolü için kullanıyor, gerçek Admin yetkisini DAL ve backend'e bırakıyorum.
export function proxy(request: NextRequest): NextResponse {
  const { pathname, search } = request.nextUrl;
  if (!isProtectedAdminPath(pathname)) return NextResponse.next();

  if (request.cookies.has(ADMIN_ACCESS_COOKIE)) return NextResponse.next();

  const returnTo = safeReturnTo(`${pathname}${search}`);
  if (request.cookies.has(ADMIN_REFRESH_COOKIE)) {
    const refreshUrl = new URL("/api/auth/refresh", siteConfig.url);
    refreshUrl.searchParams.set("returnTo", returnTo);
    return NextResponse.redirect(refreshUrl);
  }

  const loginUrl = new URL("/login", siteConfig.url);
  loginUrl.searchParams.set("returnTo", returnTo);
  loginUrl.searchParams.set("reason", "session_required");
  return NextResponse.redirect(loginUrl);
}

export const config = {
  matcher: ["/dashboard/:path*", "/products/:path*", "/orders/:path*", "/contact-messages/:path*"],
};
