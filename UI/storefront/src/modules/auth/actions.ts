"use server";

import { redirect } from "next/navigation";

import {
  clearAuthCookies,
  clearGuestSessionCookie,
  readGuestSessionCookie,
  readRefreshToken,
  writeAuthCookies,
} from "@/lib/auth/cookies";
import { safeReturnTo } from "@/lib/auth/policy";
import { ApiError } from "@/lib/api/problem";
import { claimGuestSession, loginCustomer, logoutCustomer, registerCustomer } from "@/modules/auth/api";
import type { AuthActionState } from "@/modules/auth/state";
import { validateLogin, validateRegister, type AuthFieldErrors, type AuthFormValues } from "@/modules/auth/validation";

// Burada login formunu doğrulayıp ortak oturum kurma akışından sonra güvenli dönüş hedefine yönlendiriyorum.
export async function loginAction(state: AuthActionState, formData: FormData): Promise<AuthActionState> {
  const revision = state.revision + 1;
  const validation = validateLogin(formData);
  if (!validation.success) {
    return { status: "error", revision, message: "Lütfen işaretli alanları kontrol edin.", fieldErrors: validation.errors, values: validation.values };
  }

  let destination = "/";
  try {
    await establishCustomerSession(validation.data);
    destination = safeReturnTo(formData.get("returnTo"));
  } catch (error) {
    return authErrorState(error, revision, { email: validation.data.email }, "E-posta veya şifre hatalı.");
  }

  redirect(destination);
}

// Burada yeni kullanıcıyı kaydettikten sonra aynı sunucu akışında oturumunu kurup ana sayfaya yönlendiriyorum.
export async function registerAction(state: AuthActionState, formData: FormData): Promise<AuthActionState> {
  const revision = state.revision + 1;
  const validation = validateRegister(formData);
  if (!validation.success) {
    return { status: "error", revision, message: "Lütfen işaretli alanları kontrol edin.", fieldErrors: validation.errors, values: validation.values };
  }

  try {
    await registerCustomer(validation.data);
  } catch (error) {
    const { firstName, lastName, email, phoneNumber } = validation.data;
    return authErrorState(
      error,
      revision,
      { firstName, lastName, email, phoneNumber: phoneNumber ?? "" },
      "Hesap şu anda oluşturulamadı.",
    );
  }

  try {
    await establishCustomerSession({
      email: validation.data.email,
      password: validation.data.password,
    });
  } catch {
    redirect("/login?registered=1&autoLogin=failed");
  }

  redirect("/");
}

// Burada backend refresh oturumunu mümkünse iptal edip sonuçtan bağımsız olarak yerel çerezleri temizleyerek login ekranına dönüyorum.
export async function logoutAction(): Promise<void> {
  const refreshToken = await readRefreshToken();
  try {
    if (refreshToken) await logoutCustomer(refreshToken);
  } catch {
    // Burada upstream logout hatasında kullanıcıyı yerel oturumda kilitli bırakmamak için temizleme adımına devam ediyorum.
  } finally {
    await clearAuthCookies();
  }

  redirect("/login?loggedOut=1");
}

// Burada login ve kayıt sonrası ortak HttpOnly guest sessionı cart ve favorites için yalnız bir kez claim ediyorum.
async function establishCustomerSession(credentials: { email: string; password: string }): Promise<void> {
  const result = await loginCustomer(credentials);
  await writeAuthCookies(result.tokens);

  const guestSessionToken = await readGuestSessionCookie();
  if (guestSessionToken && await claimGuestSession(result.tokens.accessToken, guestSessionToken)) {
    await clearGuestSessionCookie();
  }
}

// Burada API ProblemDetails cevabını hassas ayrıntıları sızdırmadan kullanıcıya uygun form durumuna çeviriyorum.
function authErrorState(error: unknown, revision: number, values: AuthFormValues, fallback: string): AuthActionState {
  if (!(error instanceof ApiError)) {
    return { status: "error", revision, message: "Bağlantı kurulamadı. Lütfen biraz sonra tekrar deneyin.", values };
  }

  if (error.problem.status === 429) {
    return { status: "error", revision, message: "Çok fazla deneme yapıldı. Lütfen kısa bir süre sonra tekrar deneyin.", values };
  }
  if (error.problem.status === 409) {
    return { status: "error", revision, message: "Bu e-posta adresiyle zaten bir hesap bulunuyor.", fieldErrors: { email: "Giriş yapmayı deneyebilirsiniz." }, values };
  }
  if (error.problem.status === 401) {
    return { status: "error", revision, message: "E-posta veya şifre hatalı.", values };
  }
  if (error.problem.status === 400 && error.problem.errors) {
    return { status: "error", revision, message: "Lütfen işaretli alanları kontrol edin.", fieldErrors: apiFieldErrors(error.problem.errors), values };
  }
  return { status: "error", revision, message: fallback, values };
}

function apiFieldErrors(errors: Record<string, string[]>): AuthFieldErrors {
  const allowed = new Set(["email", "password", "firstname", "lastname", "phonenumber"]);
  return Object.fromEntries(
    Object.entries(errors).flatMap(([key, messages]) => {
      const normalized = key.toLowerCase();
      if (!allowed.has(normalized) || !messages[0]) return [];
      const clientKey = normalized === "firstname" ? "firstName" : normalized === "lastname" ? "lastName" : normalized === "phonenumber" ? "phoneNumber" : normalized;
      return [[clientKey, messages[0]]];
    }),
  ) as AuthFieldErrors;
}
