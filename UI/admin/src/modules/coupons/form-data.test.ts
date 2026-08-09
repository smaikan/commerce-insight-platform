import { describe, expect, it } from "vitest";
import { parseCouponForm } from "./form-data";

function formData(values: Record<string, string>): FormData {
  const data = new FormData();
  for (const [key, value] of Object.entries(values)) data.set(key, value);
  return data;
}

describe("coupon form", () => {
  // Burada yüzde kuponunun API isteğine doğru biçimde dönüştüğünü doğruluyorum.
  it("normalizes a valid percentage coupon", () => {
    const result = parseCouponForm(formData({ code: " yaz10 ", discountType: "0", discountValue: "10", isActive: "on", isMemberOnly: "on" }));
    expect(result).toEqual({ ok: true, value: { code: "YAZ10", description: null, discountType: 0, discountValue: 10, minimumOrderAmount: null, usageLimit: null, startsAt: null, expiresAt: null, isActive: true, isMemberOnly: true } });
  });

  // Burada yüzde değerinin sözleşme dışı bir değerle kaydedilmediğini doğruluyorum.
  it("rejects a percentage above one hundred", () => {
    const result = parseCouponForm(formData({ code: "SAVE", discountType: "0", discountValue: "101", isActive: "on" }));
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.fieldErrors.discountValue).toContain("Yüzde indirimi 100'ü aşamaz.");
  });

  // Burada ters tarih aralığının formda erken yakalandığını doğruluyorum.
  it("rejects an inverted date range", () => {
    const result = parseCouponForm(formData({ code: "SAVE", discountType: "1", discountValue: "100", startsAt: "2026-08-10T12:00", expiresAt: "2026-08-09T12:00", isActive: "on" }));
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.fieldErrors.expiresAt).toContain("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
  });
});
