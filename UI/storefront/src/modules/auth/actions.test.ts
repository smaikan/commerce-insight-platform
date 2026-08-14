import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  registerCustomer: vi.fn(),
  loginCustomer: vi.fn(),
  claimGuestSession: vi.fn(),
  writeAuthCookies: vi.fn(),
  readGuestSessionCookie: vi.fn(),
  clearGuestSessionCookie: vi.fn(),
  logoutCustomer: vi.fn(),
  readRefreshToken: vi.fn(),
  clearAuthCookies: vi.fn(),
  redirect: vi.fn(),
}));

vi.mock("next/navigation", () => ({ redirect: mocks.redirect }));
vi.mock("@/modules/auth/api", () => ({
  registerCustomer: mocks.registerCustomer,
  loginCustomer: mocks.loginCustomer,
  claimGuestSession: mocks.claimGuestSession,
  logoutCustomer: mocks.logoutCustomer,
}));
vi.mock("@/lib/auth/cookies", () => ({
  writeAuthCookies: mocks.writeAuthCookies,
  readGuestSessionCookie: mocks.readGuestSessionCookie,
  clearGuestSessionCookie: mocks.clearGuestSessionCookie,
  readRefreshToken: mocks.readRefreshToken,
  clearAuthCookies: mocks.clearAuthCookies,
}));

import { loginAction, logoutAction, registerAction } from "./actions";
import { initialAuthState } from "./state";

const tokens = {
  accessToken: "access-token",
  accessTokenExpiresAt: "2026-08-13T10:15:00Z",
  refreshToken: "refresh-token",
  refreshTokenExpiresAt: "2026-08-20T10:00:00Z",
};

beforeEach(() => {
  vi.clearAllMocks();
  mocks.registerCustomer.mockResolvedValue({ user: {} });
  mocks.loginCustomer.mockResolvedValue({ user: {}, tokens });
  mocks.readGuestSessionCookie.mockResolvedValue(null);
  mocks.readRefreshToken.mockResolvedValue("refresh-token");
  mocks.logoutCustomer.mockResolvedValue(undefined);
  mocks.redirect.mockImplementation((destination: string) => {
    throw new Error(`redirect:${destination}`);
  });
});

describe("logout action", () => {
  // Burada çıkışın backend refresh oturumunu iptal edip yerel çerezleri temizledikten sonra login ekranına döndüğünü doğruluyorum.
  it("revokes the refresh session and clears local cookies", async () => {
    await expect(logoutAction()).rejects.toThrow("redirect:/login?loggedOut=1");

    expect(mocks.logoutCustomer).toHaveBeenCalledWith("refresh-token");
    expect(mocks.clearAuthCookies).toHaveBeenCalledOnce();
  });

  // Burada backend logout erişilemez olsa bile yerel oturum çerezlerinin temizlenmesini garanti ediyorum.
  it("clears local cookies when upstream logout fails", async () => {
    mocks.logoutCustomer.mockRejectedValueOnce(new Error("unavailable"));

    await expect(logoutAction()).rejects.toThrow("redirect:/login?loggedOut=1");

    expect(mocks.clearAuthCookies).toHaveBeenCalledOnce();
  });
});

describe("login action", () => {
  // Burada guest session bulunan login akışının claim endpointini yalnız bir kez çağırıp başarılı sonuçta cookie'yi temizlediğini doğruluyorum.
  it("claims the guest session exactly once after login", async () => {
    mocks.readGuestSessionCookie.mockResolvedValue("A".repeat(64));
    mocks.claimGuestSession.mockResolvedValue({ cart: { items: [] }, favoriteCount: 2 });
    const formData = new FormData();
    formData.set("email", "ada@example.com");
    formData.set("password", "secret7");
    formData.set("returnTo", "/account/favorites");

    await expect(loginAction(initialAuthState, formData)).rejects.toThrow("redirect:/account/favorites");

    expect(mocks.claimGuestSession).toHaveBeenCalledOnce();
    expect(mocks.clearGuestSessionCookie).toHaveBeenCalledOnce();
  });
});

describe("register action", () => {
  // Burada başarılı kaydın aynı kimlik bilgileriyle otomatik login yapıp HttpOnly oturumu kurduktan sonra ana sayfaya yöneldiğini doğruluyorum.
  it("creates a session and redirects home after registration", async () => {
    await expect(registerAction(initialAuthState, validRegistration())).rejects.toThrow("redirect:/");

    expect(mocks.registerCustomer).toHaveBeenCalledOnce();
    expect(mocks.loginCustomer).toHaveBeenCalledWith({ email: "ada@example.com", password: "secret7" });
    expect(mocks.writeAuthCookies).toHaveBeenCalledWith(tokens);
    expect(mocks.redirect).toHaveBeenLastCalledWith("/");
  });

  // Burada kayıt tamamlanıp otomatik login başarısız olursa kullanıcıyı yeniden kayıt çatışmasına sokmadan manuel girişe düşürdüğümü doğruluyorum.
  it("falls back to login when automatic login fails", async () => {
    mocks.loginCustomer.mockRejectedValueOnce(new Error("login unavailable"));

    await expect(registerAction(initialAuthState, validRegistration())).rejects.toThrow(
      "redirect:/login?registered=1&autoLogin=failed",
    );

    expect(mocks.writeAuthCookies).not.toHaveBeenCalled();
    expect(mocks.redirect).toHaveBeenLastCalledWith("/login?registered=1&autoLogin=failed");
  });

  // Burada kayıt sonrasında ortak guest sessionın yalnız başarılı claim cevabıyla bir kez temizlendiğini doğruluyorum.
  it("claims and clears an existing guest session after automatic login", async () => {
    mocks.readGuestSessionCookie.mockResolvedValue("A".repeat(64));
    mocks.claimGuestSession.mockResolvedValue({ cart: { items: [] }, favoriteCount: 2 });

    await expect(registerAction(initialAuthState, validRegistration())).rejects.toThrow("redirect:/");

    expect(mocks.claimGuestSession).toHaveBeenCalledOnce();
    expect(mocks.claimGuestSession).toHaveBeenCalledWith("access-token", "A".repeat(64));
    expect(mocks.clearGuestSessionCookie).toHaveBeenCalledOnce();
  });

  // Burada claim hatasında login oturumunu sürdürüp ortak guest cookie'yi sonraki kontrollü deneme için koruyorum.
  it("keeps the guest session cookie when claim fails", async () => {
    mocks.readGuestSessionCookie.mockResolvedValue("A".repeat(64));
    mocks.claimGuestSession.mockResolvedValue(null);

    await expect(registerAction(initialAuthState, validRegistration())).rejects.toThrow("redirect:/");

    expect(mocks.claimGuestSession).toHaveBeenCalledOnce();
    expect(mocks.clearGuestSessionCookie).not.toHaveBeenCalled();
  });
});

function validRegistration(): FormData {
  const formData = new FormData();
  Object.entries({
    firstName: "Ada",
    lastName: "Lovelace",
    email: "ada@example.com",
    phoneNumber: "",
    password: "secret7",
    confirmPassword: "secret7",
  }).forEach(([key, value]) => formData.set(key, value));
  return formData;
}
