import { afterEach, describe, expect, it, vi } from "vitest";

const { readAccessTokenMock } = vi.hoisted(() => ({ readAccessTokenMock: vi.fn() }));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/auth/cookies", () => ({ readAccessToken: readAccessTokenMock }));
vi.mock("@/lib/api/client", () => ({ internalApiUrl: (path: string) => new URL(path, "http://api.test") }));

import { authenticatedApiRequest } from "@/lib/api/authenticated-client";

describe("authenticated API client", () => {
  afterEach(() => vi.unstubAllGlobals());

  // Burada hesap isteğinin Bearer tokenı yalnız server fetch header'ında taşıdığını ve paylaşılan cache'i kapattığını doğruluyorum.
  it("uses the access token with private no-store fetching", async () => {
    readAccessTokenMock.mockResolvedValue("secret-access-token");
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: "U00001" }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }));
    vi.stubGlobal("fetch", fetchMock);

    await authenticatedApiRequest("/api/users/me");
    const [, options] = fetchMock.mock.calls[0];
    expect(options.cache).toBe("no-store");
    expect(new Headers(options.headers).get("Authorization")).toBe("Bearer secret-access-token");
  });
});
