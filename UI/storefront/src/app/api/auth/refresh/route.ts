import { NextRequest, NextResponse } from "next/server";

import { clearAuthCookies, readRefreshToken, writeAuthCookies } from "@/lib/auth/cookies";
import { safeReturnTo } from "@/lib/auth/policy";
import { refreshCustomerSession } from "@/modules/auth/api";

// Burada Server Component renderında yenilenemeyen oturumu cookie yazabilen kontrollü HTTP sınırında döndürüyorum.
export async function GET(request: NextRequest) {
  const returnTo = safeReturnTo(request.nextUrl.searchParams.get("returnTo"));
  const refreshToken = await readRefreshToken();

  if (refreshToken) {
    try {
      const result = await refreshCustomerSession(refreshToken);
      await writeAuthCookies(result.tokens);
      return NextResponse.redirect(new URL(returnTo, request.url));
    } catch {
      // Burada geçersiz veya süresi dolmuş refresh oturumunu yerel çerezlerde bırakmadan login akışına geçiyorum.
    }
  }

  await clearAuthCookies();
  const loginUrl = new URL("/login", request.url);
  loginUrl.searchParams.set("returnTo", returnTo);
  loginUrl.searchParams.set("sessionExpired", "1");
  return NextResponse.redirect(loginUrl);
}
