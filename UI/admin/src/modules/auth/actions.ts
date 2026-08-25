"use server";

import { redirect } from "next/navigation";
import { assertActiveAdmin } from "@/lib/auth/contracts";
import { setSessionCookies } from "@/lib/auth/cookies";
import { loginWithPassword, logoutWithToken } from "@/lib/auth/backend";
import { revokeAndClearSession, verifyAdminAccessToken } from "@/lib/auth/session";
import { safeReturnTo, validateLoginForm } from "@/lib/auth/policy";
import { loginError } from "@/modules/auth/login-error";
import type { LoginActionState } from "@/modules/auth/types";

// Burada login bilgisini server-side doğrulayıp yalnız backend tarafından tekrar doğrulanmış aktif Admin için cookie yazıyorum.
export async function loginAction(
  _previousState: LoginActionState,
  formData: FormData,
): Promise<LoginActionState> {
  const parsed = validateLoginForm(formData);
  if (!parsed.ok) {
    return {
      status: "error",
      message: "Giriş bilgilerini kontrol edin.",
      email: parsed.email,
      fieldErrors: parsed.fieldErrors,
    };
  }

  try {
    const result = await loginWithPassword(parsed.value.email, parsed.value.password);
    try {
      assertActiveAdmin(result.user);
      await verifyAdminAccessToken(result.tokens.accessToken);
    } catch (error) {
      await revokeRejectedLogin(result.tokens.refreshToken);
      throw error;
    }

    await setSessionCookies(result.tokens);
  } catch (error) {
    return loginError(error, parsed.value.email);
  }

  redirect(safeReturnTo(parsed.value.returnTo));
}

// Burada logout isteğinde upstream sonucu ne olursa olsun iki yerel token cookie'sini temizleyip login'e dönüyorum.
export async function logoutAction(): Promise<never> {
  await revokeAndClearSession();
  redirect("/login?reason=logged_out");
}

// Burada erişimi reddedilmiş oturumu POST tabanlı Server Action üzerinden temizleyip login ekranına döndürüyorum.
export async function clearRejectedSessionAction(): Promise<never> {
  await revokeAndClearSession();
  redirect("/login?reason=forbidden");
}

// Burada Customer veya pasif kullanıcı login'inin backend'de açtığı refresh oturumunu cookie yazmadan önce iptal ediyorum.
async function revokeRejectedLogin(refreshToken: string): Promise<void> {
  try {
    await logoutWithToken(refreshToken);
  } catch {
    // Burada reddedilen tokenı hiçbir zaman browser'a yazmadığım için upstream iptal hatasında da panel oturumu oluşmasına izin vermiyorum.
  }
}
