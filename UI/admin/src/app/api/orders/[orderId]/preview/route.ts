import { NextResponse } from "next/server";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { getOrder } from "@/modules/orders/api";
import { toOrderListPreview } from "@/modules/orders/preview";
import type { OrderPreviewError } from "@/modules/orders/types";

type RouteContext = { params: Promise<{ orderId: string }> };

// Burada açılan sipariş satırı için rolü yeniden doğrulayıp mevcut yönetici detay endpoint'inden daraltılmış özeti getiriyorum.
export async function GET(_request: Request, context: RouteContext): Promise<NextResponse> {
  const { orderId } = await context.params;
  try {
    const session = await requireAdminActionSession();
    if (!isUuid(orderId)) {
      return noStoreJson<OrderPreviewError>({ message: "Geçersiz sipariş kimliği." }, 400);
    }
    const order = await getOrder(orderId, session);
    return noStoreJson(toOrderListPreview(order), 200);
  } catch (error) {
    return orderPreviewErrorResponse(error);
  }
}

// Burada kullanıcı girdisi olan route parametresini backend yoluna katmadan önce UUID biçiminde doğruluyorum.
function isUuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}

// Burada upstream hata ayrıntılarını sızdırmadan 401, 403 ve 404 durumlarını eyleme dönük güvenli mesajlara çeviriyorum.
function orderPreviewErrorResponse(error: unknown): NextResponse {
  if (!(error instanceof ApiError)) {
    return noStoreJson<OrderPreviewError>({ message: "Sipariş özeti yüklenemedi. Lütfen tekrar deneyin." }, 500);
  }

  const status = error.problem.status >= 400 && error.problem.status <= 599 ? error.problem.status : 500;
  const messages: Partial<Record<number, string>> = {
    401: "Oturumunuz sona erdi. Sayfayı yenileyip tekrar giriş yapın.",
    403: "Bu sipariş bilgilerini görüntüleme yetkiniz bulunmuyor.",
    404: "Sipariş artık bulunamıyor veya kaldırılmış.",
  };

  return noStoreJson<OrderPreviewError>(
    {
      message: messages[status] ?? "Sipariş özeti yüklenemedi. Lütfen tekrar deneyin.",
      traceId: error.problem.traceId,
    },
    status,
  );
}

// Burada kişisel sipariş verisinin tarayıcıda veya paylaşılan ara katmanlarda saklanmasını engelliyorum.
function noStoreJson<T>(body: T, status: number): NextResponse {
  return NextResponse.json(body, {
    status,
    headers: {
      "Cache-Control": "private, no-store, max-age=0",
      Pragma: "no-cache",
    },
  });
}
