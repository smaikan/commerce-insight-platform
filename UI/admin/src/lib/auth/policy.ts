import { PROTECTED_ADMIN_PREFIXES } from "./constants";

export type LoginInput = {
  email: string;
  password: string;
  returnTo: string;
};

export type LoginValidationResult =
  | { ok: true; value: LoginInput }
  | { ok: false; email: string; fieldErrors: Record<string, string[]> };

// Burada login alanlarını backend sınırlarıyla aynı uzunluklarda doğrulayıp parolayı hiçbir hata durumuna geri taşımıyorum.
export function validateLoginForm(formData: FormData): LoginValidationResult {
  const email = formText(formData, "email").trim();
  const password = formText(formData, "password");
  const fieldErrors: Record<string, string[]> = {};

  if (!email) fieldErrors.email = ["E-posta adresi zorunludur."];
  else if (email.length > 320 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    fieldErrors.email = ["Geçerli bir e-posta adresi girin."];
  }

  if (!password) fieldErrors.password = ["Parola zorunludur."];
  else if (password.length > 128) fieldErrors.password = ["Parola en fazla 128 karakter olabilir."];

  if (Object.keys(fieldErrors).length > 0) return { ok: false, email, fieldErrors };
  return {
    ok: true,
    value: {
      email,
      password,
      returnTo: safeReturnTo(formText(formData, "returnTo")),
    },
  };
}

// Burada yönlendirme hedefini yalnız aynı origin altında kalan, auth döngüsü üretmeyen göreli admin yollarıyla sınırlıyorum.
export function safeReturnTo(value: string | null | undefined, fallback = "/dashboard"): string {
  const candidate = value?.trim();
  if (!candidate || !candidate.startsWith("/") || candidate.startsWith("//") || candidate.includes("\\")) {
    return fallback;
  }

  try {
    const parsed = new URL(candidate, "http://admin.internal");
    if (parsed.origin !== "http://admin.internal") return fallback;
    if (!isProtectedAdminPath(parsed.pathname)) return fallback;
    return `${parsed.pathname}${parsed.search}`;
  } catch {
    return fallback;
  }
}

// Burada oturum cookie'lerinin browser güvenlik seçeneklerini ortamdan bağımsız ve test edilebilir biçimde üretiyorum.
export function sessionCookiePolicy(expires: Date, secure: boolean) {
  return {
    httpOnly: true,
    secure,
    sameSite: "lax" as const,
    path: "/",
    expires,
    priority: "high" as const,
  };
}

// Burada Proxy'nin yalnız hızlı cookie kontrolü uygulayacağı gerçek admin route öneklerini belirliyorum.
export function isProtectedAdminPath(pathname: string): boolean {
  return PROTECTED_ADMIN_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`),
  );
}

// Burada FormData içindeki tekil metni güvenli boş varsayılanla okuyorum.
function formText(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value : "";
}
