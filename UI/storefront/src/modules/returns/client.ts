"use client";

import type { AccountOrder, AccountReturn, AccountReturnPage, ProductVariantPage } from "@/modules/account/contracts";
import type { components } from "@/generated/api";

type GuestReturnPayload = components["schemas"]["GuestReturnRequest"];
type GuestAccessExchange = components["schemas"]["GuestAccessExchangeResponse"];

// Burada misafir self-service isteklerini same-origin, no-store ve tek ProblemDetails davranışında topluyorum.
async function guestRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(path, { ...init, cache: "no-store", credentials: "same-origin" });
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    const source = body && typeof body === "object" ? body as Record<string, unknown> : {};
    throw new Error(typeof source.detail === "string" ? source.detail : typeof source.title === "string" ? source.title : "İşlem tamamlanamadı.");
  }
  return body as T;
}

export function getGuestOrder(orderId: string): Promise<AccountOrder> {
  return guestRequest(`/api/guest-orders/${encodeURIComponent(orderId)}`);
}

export function getGuestReturns(orderId: string): Promise<AccountReturnPage> {
  return guestRequest(`/api/guest-orders/${encodeURIComponent(orderId)}/returns?pageNumber=1&pageSize=50`);
}

export function getGuestReturn(orderId: string, returnId: string): Promise<AccountReturn> {
  return guestRequest(`/api/guest-orders/${encodeURIComponent(orderId)}/returns/${encodeURIComponent(returnId)}`);
}

export function createGuestReturn(orderId: string, payload: GuestReturnPayload): Promise<AccountReturn> {
  return guestRequest(`/api/guest-orders/${encodeURIComponent(orderId)}/returns`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
}

export function getGuestProductVariants(productId: string): Promise<ProductVariantPage> {
  return guestRequest(`/api/product-variants/by-product/${encodeURIComponent(productId)}`);
}

export async function requestGuestAccessLink(orderNumber: string, email: string): Promise<void> {
  await guestRequest("/api/guest-orders/access-links", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ orderNumber, email }) });
}

export function exchangeGuestAccessToken(token: string): Promise<GuestAccessExchange> {
  return guestRequest("/api/guest-orders/access/exchange", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token }) });
}
