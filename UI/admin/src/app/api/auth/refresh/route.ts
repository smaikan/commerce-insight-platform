import { NextRequest, NextResponse } from "next/server";
import { ApiError } from "@/lib/api/problem";
import { clearSessionCookies } from "@/lib/auth/cookies";
import { safeReturnTo } from "@/lib/auth/policy";
import { refreshAdminSession } from "@/lib/auth/session";

// Burada süresi dolan access cookie için refresh tokenı tek kez döndürüp yalnız doğrulanmış Admin oturumunu güvenli hedefe taşıyorum.
export async function GET(request: NextRequest): Promise<NextResponse> {
  const returnTo = safeReturnTo(request.nextUrl.searchParams.get("returnTo"));
  try {
    await refreshAdminSession();
    return noStoreRedirect(new URL(returnTo, request.url));
  } catch (error) {
    const reason = error instanceof ApiError && error.problem.status === 403
      ? "forbidden"
      : error instanceof ApiError && error.problem.status >= 500
        ? "verification_failed"
        : "session_expired";

    if (!(error instanceof ApiError) || error.problem.status < 500) await clearSessionCookies();
    return noStoreRedirect(new URL(`/login?reason=${reason}`, request.url));
  }
}

// Burada auth yönlendirmelerinin browser veya ara katman cache'lerine kaydedilmesini engelliyorum.
function noStoreRedirect(url: URL): NextResponse {
  const response = NextResponse.redirect(url, 303);
  response.headers.set("Cache-Control", "no-store, max-age=0");
  response.headers.set("Pragma", "no-cache");
  return response;
}
