import { NextRequest, NextResponse } from "next/server";
import { siteConfig } from "@/lib/site-config";
import { clearSessionCookies } from "@/lib/auth/cookies";
import { safeReturnTo } from "@/lib/auth/policy";
import { refreshFailureDecision } from "@/lib/auth/refresh-failure";
import { refreshAdminSession } from "@/lib/auth/session";

// Burada süresi dolan access cookie için refresh tokenı tek kez döndürüp yalnız doğrulanmış Admin oturumunu güvenli hedefe taşıyorum.
export async function GET(request: NextRequest): Promise<NextResponse> {
  const returnTo = safeReturnTo(request.nextUrl.searchParams.get("returnTo"));
  try {
    await refreshAdminSession();
    return noStoreRedirect(new URL(returnTo, siteConfig.url));
  } catch (error) {
    const decision = refreshFailureDecision(error);
    if (decision.clearCookies) await clearSessionCookies();

    const loginUrl = new URL("/login", siteConfig.url);
    loginUrl.searchParams.set("reason", decision.reason);
    loginUrl.searchParams.set("returnTo", returnTo);
    if (decision.retryAfter) loginUrl.searchParams.set("retryAfter", String(decision.retryAfter));

    const response = noStoreRedirect(loginUrl);
    if (decision.retryAfter) response.headers.set("Retry-After", String(decision.retryAfter));
    return response;
  }
}

// Burada auth yönlendirmelerinin browser veya ara katman cache'lerine kaydedilmesini engelliyorum.
function noStoreRedirect(url: URL): NextResponse {
  const response = NextResponse.redirect(url, 303);
  response.headers.set("Cache-Control", "no-store, max-age=0");
  response.headers.set("Pragma", "no-cache");
  return response;
}
