import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type {
  Order,
  OrderListQuery,
  OrderPage,
  ReturnRequest,
  ReturnRequestPage,
} from "@/modules/orders/types";

// Burada yönetici sipariş listesini yalnız belgelenmiş durum, UTC tarih ve sayfalama parametreleriyle getiriyorum.
export function getOrders(query: OrderListQuery, session: AdminSession): Promise<OrderPage> {
  const params = new URLSearchParams({
    PageNumber: String(query.pageNumber),
    PageSize: String(query.pageSize),
  });
  if (query.search) params.set("Search", query.search);
  if (query.status !== undefined) params.set("Status", String(query.status));
  if (query.createdFromUtc) params.set("CreatedFromUtc", query.createdFromUtc);
  if (query.createdToUtc) params.set("CreatedToUtc", query.createdToUtc);
  return apiRequest(`/api/orders?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada yönetici sipariş detayını müşteri, adres, kalem ve ödeme snapshot'larıyla server-side getiriyorum.
export function getOrder(orderId: string, session: AdminSession): Promise<Order> {
  return apiRequest(`/api/orders/admin/${encodeURIComponent(orderId)}`, { accessToken: session.accessToken });
}

// Burada sipariş durumunu yalnız belgelenmiş yönetim endpoint'i üzerinden, kargo alanlarıyla birlikte güncelliyorum.
export function updateOrderStatus(
  orderId: string,
  input: {
    status: Order["status"];
    shippingCarrier?: string | null;
    trackingNumber?: string | null;
    trackingUrl?: string | null;
  },
  session: AdminSession,
): Promise<Order> {
  return apiRequest(`/api/orders/${encodeURIComponent(orderId)}/status`, {
    method: "PATCH",
    body: input,
    accessToken: session.accessToken,
  });
}

// Burada siparişe ait tüm iade özet sayfalarını getirip ürün/adet bilgisi için yönetici detaylarını birlikte yüklüyorum.
export async function getOrderReturns(orderId: string, session: AdminSession): Promise<ReturnRequest[]> {
  const firstPage = await getOrderReturnPage(orderId, 1, session);
  const remainingPages = firstPage.totalPages > 1
    ? await Promise.all(
        Array.from({ length: firstPage.totalPages - 1 }, (_, index) => getOrderReturnPage(orderId, index + 2, session)),
      )
    : [];
  const summaries = [firstPage, ...remainingPages].flatMap((page) => page.items);
  return Promise.all(summaries.map((summary) => getReturnRequest(summary.id, session)));
}

// Burada sipariş filtresini UUID ile sınırlayarak iade özetlerini en büyük belgelenmiş sayfa boyutunda okuyorum.
function getOrderReturnPage(orderId: string, pageNumber: number, session: AdminSession): Promise<ReturnRequestPage> {
  const params = new URLSearchParams({
    PageNumber: String(pageNumber),
    PageSize: "100",
    OrderId: orderId,
  });
  return apiRequest(`/api/returns?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada iade talebinin ürün ve adet karar bağlamını yönetici detay endpoint'inden okuyorum.
export function getReturnRequest(returnRequestId: string, session: AdminSession): Promise<ReturnRequest> {
  return apiRequest(`/api/returns/admin/${encodeURIComponent(returnRequestId)}`, { accessToken: session.accessToken });
}

// Burada bekleyen iade talebinin tamamı için belgelenmiş onay veya ret kararını gönderiyorum.
export function decideReturnRequest(
  returnRequestId: string,
  intent: "approve" | "reject",
  decisionNote: string | null,
  session: AdminSession,
): Promise<ReturnRequest> {
  return apiRequest(`/api/returns/${encodeURIComponent(returnRequestId)}/${intent}`, {
    method: "POST",
    body: { decisionNote },
    accessToken: session.accessToken,
  });
}

// Burada onaylanmış iadenin teslim alma veya tamamlama geçişini ayrı yaşam döngüsü endpoint'ine gönderiyorum.
export function advanceReturnRequest(
  returnRequestId: string,
  intent: "receive" | "complete",
  session: AdminSession,
): Promise<ReturnRequest> {
  return apiRequest(`/api/returns/${encodeURIComponent(returnRequestId)}/${intent}`, {
    method: "POST",
    accessToken: session.accessToken,
  });
}
