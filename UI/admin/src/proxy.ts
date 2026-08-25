import { NextRequest, NextResponse } from "next/server";
import { siteConfig } from "@/lib/site-config";
import { ADMIN_ACCESS_COOKIE, ADMIN_REFRESH_COOKIE, ADMIN_RETURN_TO_HEADER } from "@/lib/auth/constants";
import { isProtectedAdminPath, safeReturnTo } from "@/lib/auth/policy";

// Burada Proxy'yi yalnız hızlı cookie varlığı kontrolü için kullanıyor, gerçek Admin yetkisini DAL ve backend'e bırakıyorum.
export function proxy(request: NextRequest): NextResponse {
  const { pathname, search } = request.nextUrl;
  if (!isProtectedAdminPath(pathname)) return NextResponse.next();

  const returnTo = safeReturnTo(`${pathname}${search}`);
  if (request.cookies.has(ADMIN_ACCESS_COOKIE)) {
    // Burada browser'dan gelebilecek aynı adlı headerı ezip gerçek istek adresini yalnız upstream Server Component'lere iletiyorum.
    const requestHeaders = new Headers(request.headers);
    requestHeaders.set(ADMIN_RETURN_TO_HEADER, returnTo);
    return NextResponse.next({ request: { headers: requestHeaders } });
  }

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
  // Burada Next.js'in statik matcher analizine uygun sabitlerle uygulanmış tüm Admin route köklerini kapsıyorum.
  matcher: [
    "/accounting/:path*",
    "/banners/:path*",
    "/brands/:path*",
    "/collections/:path*",
    "/contact-messages/:path*",
    "/coupons/:path*",
    "/customers/:path*",
    "/dashboard/:path*",
    "/inventory/:path*",
    "/managers/:path*",
    "/marketing/:path*",
    "/orders/:path*",
    "/products/:path*",
    "/settings/:path*",
  ],
};
