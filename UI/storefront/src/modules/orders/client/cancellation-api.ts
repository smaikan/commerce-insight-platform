"use client";

import { normalizeApiProblem, type ApiProblem } from "@/lib/api/problem";
import type {
  CustomerOrder,
  OrderCancellationAccessMode,
  OrderCancellationOperation,
  OrderCancellationResult,
} from "@/modules/orders/cancellation";

export class OrderCancellationClientError extends Error {
  readonly problem: ApiProblem;

  // Burada browser cancellation hatasını yalnız güvenli ProblemDetails alanlarıyla taşıyorum.
  constructor(problem: ApiProblem) {
    super(problem.detail || problem.title);
    this.name = "OrderCancellationClientError";
    this.problem = problem;
  }
}

// Burada member ve guest sahiplik kanallarını yalnız sabit same-origin BFF yollarına bağlıyorum.
function cancellationPath(orderId: string, accessMode: OrderCancellationAccessMode, operation = false): string {
  const encodedOrderId = encodeURIComponent(orderId);
  if (accessMode === "guest") {
    return `/api/guest-orders/${encodedOrderId}/${operation ? "cancellation" : "cancel"}`;
  }
  return `/api/checkout/orders/${encodedOrderId}/${operation ? "cancellation" : "cancel"}`;
}

// Burada iptal mutasyonunun 200 OrderDto ve 202 operasyon DTO cevaplarını HTTP statüsüyle ayrıştırıyorum.
export async function cancelCustomerOrder(
  orderId: string,
  accessMode: OrderCancellationAccessMode,
  signal?: AbortSignal,
): Promise<OrderCancellationResult> {
  const { response, body } = await cancellationRequest(cancellationPath(orderId, accessMode), { method: "POST", signal });
  if (response.status === 200 && isCustomerOrder(body, orderId)) return { kind: "completed", order: body };
  if (response.status === 202 && isCancellationOperation(body, orderId)) return { kind: "pending", operation: body };
  throw invalidCancellationResponse();
}

// Burada yalnız owner-scoped BFF üzerinden güncel cancellation operasyonunu no-store olarak okuyorum.
export async function loadCustomerOrderCancellation(
  orderId: string,
  accessMode: OrderCancellationAccessMode,
  signal?: AbortSignal,
): Promise<OrderCancellationOperation> {
  const { response, body } = await cancellationRequest(cancellationPath(orderId, accessMode, true), { method: "GET", signal });
  if (response.status === 200 && isCancellationOperation(body, orderId)) return body;
  throw invalidCancellationResponse();
}

// Burada tamamlanan operasyon sonrasında yalnız sahiplik denetimli güncel OrderDto kaydını yeniden okuyorum.
export async function loadOrderAfterCancellation(
  orderId: string,
  accessMode: OrderCancellationAccessMode,
  signal?: AbortSignal,
): Promise<CustomerOrder> {
  const encodedOrderId = encodeURIComponent(orderId);
  const path = accessMode === "guest"
    ? `/api/guest-orders/${encodedOrderId}`
    : `/api/checkout/orders/${encodedOrderId}`;
  const { response, body } = await cancellationRequest(path, { method: "GET", signal });
  if (response.status === 200 && isCustomerOrder(body, orderId)) return body;
  throw invalidCancellationResponse();
}

// Burada JSON ve ProblemDetails cevaplarını tek kez tüketip 2xx dışındaki sonuçları güvenli hataya dönüştürüyorum.
async function cancellationRequest(path: string, init: RequestInit): Promise<{ response: Response; body: unknown }> {
  let response: Response;
  try {
    response = await fetch(path, { ...init, cache: "no-store", credentials: "same-origin" });
  } catch (error) {
    if (init.signal?.aborted) throw error;
    throw new OrderCancellationClientError({ title: "İptal servisine ulaşılamıyor", status: 503, code: "cancellation_unavailable" });
  }

  const body = await response.json().catch(() => null);
  if (!response.ok) {
    throw new OrderCancellationClientError(normalizeApiProblem(response.status, body, response.headers.get("Retry-After")));
  }
  return { response, body };
}

// Burada operasyon cevabının polling için gereken güvenli alanlara ve istenen siparişe ait olduğunu doğruluyorum.
function isCancellationOperation(value: unknown, orderId: string): value is OrderCancellationOperation {
  if (!value || typeof value !== "object") return false;
  const source = value as Partial<OrderCancellationOperation>;
  return source.orderId?.toLowerCase() === orderId.toLowerCase()
    && typeof source.operationId === "string"
    && typeof source.status === "number"
    && source.status >= 0
    && source.status <= 5
    && (source.reversalType === 0 || source.reversalType === 1)
    && typeof source.pollingUrl === "string";
}

// Burada iptal tamamlanma cevabının istenen siparişe ait temel OrderDto kimlik ve durum alanlarını taşıdığını doğruluyorum.
function isCustomerOrder(value: unknown, orderId: string): value is CustomerOrder {
  if (!value || typeof value !== "object") return false;
  const source = value as Partial<CustomerOrder>;
  return source.id?.toLowerCase() === orderId.toLowerCase()
    && typeof source.status === "number"
    && typeof source.orderNumber === "string";
}

function invalidCancellationResponse(): OrderCancellationClientError {
  return new OrderCancellationClientError({
    title: "Geçersiz iptal cevabı",
    status: 502,
    code: "invalid_cancellation_response",
    detail: "İptal servisinden beklenen cevap alınamadı.",
  });
}
