import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { Coupon, CouponListQuery, CouponPage, CouponRequest } from "@/modules/coupons/types";

// Burada kupon listesini yalnız belgelenen aktiflik ve sayfalama parametreleriyle okuyorum.
export function getCoupons(query: CouponListQuery, session: AdminSession): Promise<CouponPage> {
  const params = new URLSearchParams({ pageNumber: String(query.pageNumber), pageSize: String(query.pageSize) });
  if (query.isActive !== undefined) params.set("isActive", String(query.isActive));
  return apiRequest<CouponPage>(`/api/coupons?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada yeni kuponu yönetici oturumu altında backend'in otoriter doğrulamasına gönderiyorum.
export function createCoupon(payload: CouponRequest, session: AdminSession): Promise<Coupon> {
  return apiRequest<Coupon>("/api/coupons", { method: "POST", body: payload, accessToken: session.accessToken });
}

// Burada mevcut kuponun desteklenen alanlarını tek PUT sözleşmesiyle güncelliyorum.
export function updateCoupon(id: string, payload: CouponRequest, session: AdminSession): Promise<Coupon> {
  return apiRequest<Coupon>(`/api/coupons/${encodeURIComponent(id)}`, { method: "PUT", body: payload, accessToken: session.accessToken });
}

// Burada liste üzerinden aktiflik durumunu ayrı ve dar API işlemiyle değiştiriyorum.
export function setCouponActivation(id: string, isActive: boolean, session: AdminSession): Promise<Coupon> {
  return apiRequest<Coupon>(`/api/coupons/${encodeURIComponent(id)}/activation`, { method: "PATCH", body: { isActive }, accessToken: session.accessToken });
}
