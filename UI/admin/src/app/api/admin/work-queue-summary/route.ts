import { NextResponse } from "next/server";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { getAdminWorkQueueSummary } from "@/modules/dashboard/api";

// Burada admin rolünü yeniden doğrulayıp iş kuyruğu sayaçlarını aynı origin üzerinden güvenle sunuyorum.
export async function GET(): Promise<NextResponse> {
  try {
    const session = await requireAdminActionSession();
    const summary = await getAdminWorkQueueSummary(session);
    return noStoreJson(summary, 200);
  } catch (error) {
    return workQueueErrorResponse(error);
  }
}

// Burada upstream ayrıntılarını sızdırmadan kimlik ve servis hatalarını istemciye sınırlı biçimde çeviriyorum.
function workQueueErrorResponse(error: unknown): NextResponse {
  if (!(error instanceof ApiError)) {
    return noStoreJson({ message: "Bildirim sayaçları şu anda yenilenemiyor." }, 500);
  }

  const status = error.problem.status === 401 || error.problem.status === 403
    ? error.problem.status
    : 503;
  const message = status === 401
    ? "Oturumunuz sona erdi. Sayfayı yenileyip tekrar giriş yapın."
    : status === 403
      ? "Bildirim sayaçlarını görüntüleme yetkiniz bulunmuyor."
      : "Bildirim sayaçları şu anda yenilenemiyor.";

  return noStoreJson({ message }, status);
}

// Burada operasyon sayaçlarının tarayıcıda veya paylaşılan ara katmanlarda saklanmasını engelliyorum.
function noStoreJson<T>(body: T, status: number): NextResponse {
  return NextResponse.json(body, {
    status,
    headers: {
      "Cache-Control": "private, no-store, max-age=0",
      Pragma: "no-cache",
    },
  });
}
