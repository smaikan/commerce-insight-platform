import "server-only";

import { apiPost } from "@/lib/api/client";
import type { components } from "@/generated/api";
import {
  parseAuthResult,
  parseRegisterResult,
  type AuthResult,
  type LoginPayload,
  type RegisterPayload,
  type RegisterResult,
} from "@/modules/auth/contracts";

const DEVICE_NAME = "Storefront Web";
export type GuestSessionClaim = components["schemas"]["GuestSessionClaimDto"];

// Burada login isteğini yalnızca sunucudan gönderip çalışma zamanında doğrulanmış oturum verisini döndürüyorum.
export async function loginCustomer(payload: Omit<LoginPayload, "deviceName">): Promise<AuthResult> {
  return parseAuthResult(await apiPost<unknown>("/api/auth/login", { ...payload, deviceName: DEVICE_NAME }));
}

// Burada süresi dolan access oturumunu dönen iki yeni tokenı da doğrulayarak yeniliyorum.
export async function refreshCustomerSession(refreshToken: string): Promise<AuthResult> {
  return parseAuthResult(await apiPost<unknown>("/api/auth/refresh-token", {
    refreshToken,
    deviceName: DEVICE_NAME,
  }));
}

// Burada kullanıcı kaydını üretilen OpenAPI girdi tipiyle sunucudan gönderiyorum.
export async function registerCustomer(payload: RegisterPayload): Promise<RegisterResult> {
  return parseRegisterResult(await apiPost<unknown>("/api/auth/register", payload));
}

// Burada refresh oturumunu backend tarafında geçersizleştirip gövdesiz başarı cevabını ortak API sınırından geçiriyorum.
export async function logoutCustomer(refreshToken: string): Promise<void> {
  await apiPost<void>("/api/auth/logout", { refreshToken });
}

// Burada başarılı login sonrasında ortak guest cart ve favorites sessionını bearer tokenla tek atomik çağrıda claim ediyorum.
export async function claimGuestSession(accessToken: string, guestSessionToken: string): Promise<GuestSessionClaim | null> {
  try {
    return await apiPost<GuestSessionClaim>("/api/guest-session/claim", undefined, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        Cookie: `ecommerce_guest_cart=${guestSessionToken}`,
      },
    });
  } catch {
    return null;
  }
}
