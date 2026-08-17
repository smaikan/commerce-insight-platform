"use server";

import { ApiError } from "@/lib/api/problem";
import { createMemberOrder } from "@/modules/checkout/api";
import { parseMemberCheckoutRequest } from "@/modules/checkout/request";
import type { MemberCheckoutActionResult } from "@/modules/checkout/types";

// Burada üye checkout girdisini sunucu sınırında tekrar doğrulayıp JWT'yi browser'a açmadan sipariş oluşturuyorum.
export async function createMemberOrderAction(value: unknown): Promise<MemberCheckoutActionResult> {
  const payload = parseMemberCheckoutRequest(value);
  if (!payload) {
    return { ok: false, problem: { status: 400, title: "Geçersiz sipariş isteği", detail: "Adres, kargo ve sepet bilgilerini kontrol edin.", code: "validation_error" } };
  }

  try {
    return { ok: true, order: await createMemberOrder(payload) };
  } catch (error) {
    if (error instanceof ApiError) return { ok: false, problem: error.problem };
    return { ok: false, problem: { status: 503, title: "Sipariş oluşturulamadı", detail: "Lütfen kısa bir süre sonra tekrar deneyin.", code: "checkout_unavailable" } };
  }
}
