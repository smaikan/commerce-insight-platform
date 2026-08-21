import { afterEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "@/lib/api/problem";
import { submitContactMessage } from "@/modules/contact/client";

const request = { name: "Ada Lovelace", email: "ada@example.com", phone: null, subject: 0 as const, orderNumber: null, message: "Siparişim hakkında bilgi almak istiyorum." };

describe("contact browser client", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("submits to the same-origin BFF with intent and challenge headers", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ referenceNumber: "CNT-20260821-000001", submittedAt: "2026-08-21T12:00:00Z" }), { status: 202, headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(submitContactMessage(request, "contact-fixture", "turnstile-fixture")).resolves.toMatchObject({ referenceNumber: "CNT-20260821-000001" });
    expect(fetchMock).toHaveBeenCalledWith("/api/contact-messages", expect.objectContaining({ method: "POST", cache: "no-store" }));
    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Record<string, string>;
    expect(headers).toMatchObject({ "Idempotency-Key": "contact-fixture", "X-Turnstile-Token": "turnstile-fixture" });
  });

  it("preserves ProblemDetails and Retry-After without automatic retry", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: "Çok fazla istek", status: 429, code: "rate_limit_exceeded" }), { status: 429, headers: { "Content-Type": "application/problem+json", "Retry-After": "45" } }));
    vi.stubGlobal("fetch", fetchMock);

    const error = await submitContactMessage(request, "contact-fixture").catch((value) => value);
    expect(error).toBeInstanceOf(ApiError);
    expect(error.problem).toMatchObject({ status: 429, code: "rate_limit_exceeded", retryAfter: "45" });
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it("fails closed when a successful upstream response does not match the receipt contract", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("{}", { status: 202, headers: { "Content-Type": "application/json" } })));
    await expect(submitContactMessage(request, "contact-fixture")).rejects.toMatchObject({ problem: { code: "contact_invalid_response", status: 502 } });
  });
});
