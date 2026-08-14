import { describe, expect, it, vi } from "vitest";

import { consumeResetToken } from "@/modules/auth/password-reset-fragment";

describe("password reset fragment", () => {
  // Burada tokenın fragmenttan çözülüp sorgu dizesine eklenmeden adres çubuğunun temizlendiğini doğruluyorum.
  it("consumes one fragment token and preserves only the safe URL parts", () => {
    const replaceState = vi.fn();

    const token = consumeResetToken(
      { hash: "#token=fixture%2Btoken", pathname: "/reset-password", search: "?source=email" },
      { state: null, replaceState },
    );

    expect(token).toBe("fixture+token");
    expect(replaceState).toHaveBeenCalledWith(null, "", "/reset-password?source=email");
    expect(replaceState.mock.calls[0]?.[2]).not.toContain("token");
  });

  // Burada token bulunmadığında API akışına aktarılabilecek bir değer üretilmediğini doğruluyorum.
  it("returns null when the fragment has no token", () => {
    const replaceState = vi.fn();

    expect(consumeResetToken(
      { hash: "#source=email", pathname: "/reset-password", search: "" },
      { state: null, replaceState },
    )).toBeNull();
    expect(replaceState).toHaveBeenCalledWith(null, "", "/reset-password");
  });

  // Burada birden fazla token değerini belirsiz ve güvensiz kabul ederek reddettiğimi doğruluyorum.
  it("rejects duplicate token values", () => {
    expect(consumeResetToken(
      { hash: "#token=first&token=second", pathname: "/reset-password", search: "" },
      { state: null, replaceState: vi.fn() },
    )).toBeNull();
  });
});
