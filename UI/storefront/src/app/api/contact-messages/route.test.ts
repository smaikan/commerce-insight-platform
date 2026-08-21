import { beforeEach, describe, expect, it, vi } from "vitest";

const { trustedOriginMock, forwardMock, problemMock } = vi.hoisted(() => ({
  trustedOriginMock: vi.fn(),
  forwardMock: vi.fn(),
  problemMock: vi.fn((status: number, title: string, detail: string, code: string, errors?: Record<string, string[]>) => Response.json({ status, title, detail, code, errors }, { status })),
}));

vi.mock("@/lib/security/storefront-origin", () => ({ hasTrustedStorefrontOrigin: trustedOriginMock }));
vi.mock("@/modules/contact/server/contact-proxy", () => ({ contactProblemResponse: problemMock, forwardContactSubmission: forwardMock }));

import { POST } from "./route";

const validBody = { name: "Ada Lovelace", email: "ada@example.com", phone: null, subject: 1, orderNumber: null, message: "Ürün stoğu hakkında bilgi almak istiyorum." };

describe("contact BFF route", () => {
  beforeEach(() => {
    trustedOriginMock.mockReset().mockReturnValue(true);
    forwardMock.mockReset().mockResolvedValue(new Response(null, { status: 202 }));
    problemMock.mockClear();
  });

  it("validates and forwards only normalized contract data and allowlisted headers", async () => {
    const request = new Request("http://localhost/api/contact-messages", { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": "contact-fixture", "X-Turnstile-Token": "challenge-fixture" }, body: JSON.stringify(validBody) });
    const response = await POST(request);

    expect(response.status).toBe(202);
    expect(forwardMock).toHaveBeenCalledWith(validBody, "contact-fixture", "challenge-fixture");
  });

  it("rejects untrusted origins before reading or forwarding the body", async () => {
    trustedOriginMock.mockReturnValue(false);
    const response = await POST(new Request("http://localhost/api/contact-messages", { method: "POST", body: JSON.stringify(validBody) }));
    expect(response.status).toBe(403);
    expect(forwardMock).not.toHaveBeenCalled();
  });

  it("returns a controlled 400 response for malformed JSON", async () => {
    const response = await POST(new Request("http://localhost/api/contact-messages", { method: "POST", headers: { "Idempotency-Key": "contact-fixture" }, body: "{" }));
    expect(response.status).toBe(400);
    expect(problemMock).toHaveBeenCalledWith(400, "Geçersiz istek", expect.any(String), "bad_request");
    expect(forwardMock).not.toHaveBeenCalled();
  });
});
