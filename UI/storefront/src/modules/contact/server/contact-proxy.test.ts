import { beforeEach, describe, expect, it, vi } from "vitest";

const { readAccessTokenMock } = vi.hoisted(() => ({ readAccessTokenMock: vi.fn() }));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/auth/cookies", () => ({ readAccessToken: readAccessTokenMock }));
vi.mock("@/lib/api/client", () => ({ internalApiUrl: (path: string) => `https://api.test${path}` }));
vi.mock("@/lib/site-config", () => ({ siteConfig: { url: "https://store.test" } }));

import { forwardContactSubmission } from "@/modules/contact/server/contact-proxy";

const request = { name: "Ada Lovelace", email: "ada@example.com", phone: null, subject: 0 as const, orderNumber: null, message: "Siparişim hakkında bilgi almak istiyorum." };

describe("contact server proxy", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    readAccessTokenMock.mockReset().mockResolvedValue(null);
  });

  it("forwards only allowlisted headers and the optional authenticated owner token", async () => {
    readAccessTokenMock.mockResolvedValue("customer-access-token");
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(JSON.stringify({ referenceNumber: "CNT-20260821-000001", submittedAt: "2026-08-21T12:00:00Z" }), { status: 202, headers: { "Content-Type": "application/json" } }));

    const response = await forwardContactSubmission(request, "contact-fixture", "challenge-fixture");
    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Headers;

    expect(response.status).toBe(202);
    expect(headers.get("Authorization")).toBe("Bearer customer-access-token");
    expect(headers.get("Idempotency-Key")).toBe("contact-fixture");
    expect(headers.get("X-Turnstile-Token")).toBe("challenge-fixture");
    expect(headers.get("Origin")).toBe("https://store.test");
  });

  it("preserves upstream rate-limit metadata in the safe BFF response", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(JSON.stringify({ title: "Too many requests", status: 429, code: "rate_limit_exceeded" }), { status: 429, headers: { "Content-Type": "application/problem+json", "Retry-After": "30" } }));

    const response = await forwardContactSubmission(request, "contact-fixture");
    expect(response.status).toBe(429);
    expect(response.headers.get("Retry-After")).toBe("30");
    await expect(response.json()).resolves.toMatchObject({ code: "rate_limit_exceeded" });
  });

  it("returns a controlled 503 response when the upstream API is unavailable", async () => {
    vi.spyOn(globalThis, "fetch").mockRejectedValue(new Error("network unavailable"));
    const response = await forwardContactSubmission(request, "contact-fixture");
    expect(response.status).toBe(503);
    await expect(response.json()).resolves.toMatchObject({ code: "contact_unavailable" });
  });
});
