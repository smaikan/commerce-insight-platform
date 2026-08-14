import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  readRefreshToken: vi.fn(),
  writeAuthCookies: vi.fn(),
  clearAuthCookies: vi.fn(),
  refreshCustomerSession: vi.fn(),
  claimGuestSession: vi.fn(),
}));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/auth/cookies", () => ({
  readRefreshToken: mocks.readRefreshToken,
  writeAuthCookies: mocks.writeAuthCookies,
  clearAuthCookies: mocks.clearAuthCookies,
}));
vi.mock("@/modules/auth/api", () => ({
  refreshCustomerSession: mocks.refreshCustomerSession,
  claimGuestSession: mocks.claimGuestSession,
}));

import { NextRequest } from "next/server";

import { GET } from "./route";

describe("auth refresh route", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.readRefreshToken.mockResolvedValue("refresh-token");
    mocks.refreshCustomerSession.mockResolvedValue({
      tokens: {
        accessToken: "access-token",
        accessTokenExpiresAt: "2026-08-14T12:00:00Z",
        refreshToken: "next-refresh-token",
        refreshTokenExpiresAt: "2026-08-21T12:00:00Z",
      },
    });
  });

  // Burada refresh-token akışının auth cookie'lerini yenilerken guest session claim işlemini tekrar çalıştırmadığını doğruluyorum.
  it("does not claim the guest session during refresh", async () => {
    const response = await GET(new NextRequest("http://localhost:3000/api/auth/refresh?returnTo=/products"));

    expect(response.status).toBe(307);
    expect(response.headers.get("location")).toBe("http://localhost:3000/products");
    expect(mocks.writeAuthCookies).toHaveBeenCalledOnce();
    expect(mocks.claimGuestSession).not.toHaveBeenCalled();
  });
});
