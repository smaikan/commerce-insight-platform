import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { Order, OrderListQuery, OrderPage } from "@/modules/orders/types";

// Burada yönetici sipariş listesini yalnız belgelenmiş durum, UTC tarih ve sayfalama parametreleriyle getiriyorum.
export function getOrders(query: OrderListQuery, session: AdminSession): Promise<OrderPage> {
  const params = new URLSearchParams({
    PageNumber: String(query.pageNumber),
    PageSize: String(query.pageSize),
  });
  if (query.status !== undefined) params.set("Status", String(query.status));
  if (query.createdFromUtc) params.set("CreatedFromUtc", query.createdFromUtc);
  if (query.createdToUtc) params.set("CreatedToUtc", query.createdToUtc);
  return apiRequest(`/api/orders?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada yönetici sipariş detayını müşteri, adres, kalem ve ödeme snapshot'larıyla server-side getiriyorum.
export function getOrder(orderId: string, session: AdminSession): Promise<Order> {
  return apiRequest(`/api/orders/admin/${encodeURIComponent(orderId)}`, { accessToken: session.accessToken });
}
