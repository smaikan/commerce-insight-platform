import { describe, expect, it } from "vitest";

import { mapApiFieldErrors, parseContactSubmission, validateContactDraft } from "@/modules/contact/validation";

describe("contact submission validation", () => {
  it("normalizes a valid draft to the generated API contract", () => {
    const result = validateContactDraft({
      name: "  Ada Lovelace  ",
      email: " ADA@EXAMPLE.COM ",
      phone: " ",
      subject: 2,
      orderNumber: " ORD-20260821-000001 ",
      message: " İade sürecim hakkında bilgi almak istiyorum. ",
    });

    expect(result).toMatchObject({
      ok: true,
      value: {
        name: "Ada Lovelace",
        email: "ada@example.com",
        phone: null,
        subject: 2,
        orderNumber: "ORD-20260821-000001",
        message: "İade sürecim hakkında bilgi almak istiyorum.",
      },
    });
  });

  it("rejects unsafe, short and non-numeric contract values", () => {
    const result = parseContactSubmission({
      name: "<b>Ada</b>",
      email: "geçersiz",
      subject: "2",
      message: "kısa",
    });

    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.errors).toMatchObject({ name: expect.any(Array), email: expect.any(Array), subject: expect.any(Array), message: expect.any(Array) });
  });

  it("maps case-insensitive API validation paths to form fields", () => {
    expect(mapApiFieldErrors({ "request.OrderNumber": ["Sipariş bulunamadı."], Message: ["Mesaj geçersiz."], Unknown: ["ignored"] })).toEqual({
      orderNumber: ["Sipariş bulunamadı."],
      message: ["Mesaj geçersiz."],
    });
  });
});
