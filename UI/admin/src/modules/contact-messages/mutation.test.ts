import { describe, expect, it } from "vitest";
import { ApiError } from "../../lib/api/problem";
import { contactMessageMutationError, contactReplyIntentAfterEdit, createContactReplyIdempotencyKey, normalizeContactFieldErrors, preserveContactDraftOnConflict } from "./mutation";

describe("contact message mutations", () => {
  it("separates idempotency reuse, concurrency and rate-limit problems", () => {
    const reused = contactMessageMutationError(new ApiError({ title: "Conflict", status: 409, code: "idempotency_key_reused" }), "fallback");
    const concurrency = contactMessageMutationError(new ApiError({ title: "Conflict", status: 409, code: "concurrency_conflict" }), "fallback");
    const rate = contactMessageMutationError(new ApiError({ title: "Slow", status: 429, retryAfter: "30 saniye" }), "fallback");
    expect(reused.status === "error" && reused.message).toContain("gönderim anahtarı");
    expect(concurrency.status === "error" && concurrency.message).toBe("fallback");
    expect(rate.status === "error" && rate.retryAfter).toBe("30 saniye");
  });

  it("preserves a reply key for retry and rotates it only after body edit", () => {
    const original = { key: "CONTACT_REPLY_ORIGINAL", attemptedBody: "Aynı metin" };
    expect(contactReplyIntentAfterEdit(original, "Aynı metin")).toBe(original);
    const edited = contactReplyIntentAfterEdit(original, "Yeni metin", () => "11111111-1111-4111-8111-111111111111");
    expect(edited.key).not.toBe(original.key);
    expect(edited.attemptedBody).toBeUndefined();
    expect(createContactReplyIdempotencyKey(() => "11111111-1111-4111-8111-111111111111")).toBe("CONTACT_REPLY_11111111111141118111111111111111");
  });

  it("preserves a 5000 character draft when accepting a fresh concurrency snapshot", () => {
    const draft = "İ".repeat(5_000);
    const snapshot = { concurrencyToken: "11111111-1111-4111-8111-111111111111", status: 1 as const, assignedAdminUserId: "U00001", updatedAt: "2026-08-21T10:00:00Z" };
    expect(preserveContactDraftOnConflict(draft, snapshot)).toEqual({ draft, snapshot });
  });

  it("binds PascalCase ProblemDetails field keys to persistent controls", () => {
    expect(normalizeContactFieldErrors({ Note: ["Not zorunlu."], Body: ["Yanıt zorunlu."] })).toEqual({ note: ["Not zorunlu."], body: ["Yanıt zorunlu."] });
  });
});
