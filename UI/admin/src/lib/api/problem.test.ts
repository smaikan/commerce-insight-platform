import { describe, expect, it } from "vitest";
import { retryAfterSeconds } from "./problem";

describe("API problem Retry-After", () => {
  // Burada delta-seconds biçimini kullanıcıya gösterilecek sınırlı bekleme süresine dönüştürdüğümü doğruluyorum.
  it("parses and caps delta seconds", () => {
    expect(retryAfterSeconds("45")).toBe(45);
    expect(retryAfterSeconds("900")).toBe(300);
  });

  // Burada HTTP tarihi biçimini sabit saat üzerinden saniyeye çevirdiğimi doğruluyorum.
  it("parses an HTTP date", () => {
    const now = Date.parse("2026-08-25T10:00:00Z");
    expect(retryAfterSeconds("Tue, 25 Aug 2026 10:00:30 GMT", now)).toBe(30);
  });

  // Burada bozuk veya geçmiş Retry-After değerlerini kullanıcı mesajına taşımadığımı doğruluyorum.
  it("rejects invalid and expired values", () => {
    expect(retryAfterSeconds("invalid")).toBeUndefined();
    expect(retryAfterSeconds("0")).toBeUndefined();
    expect(retryAfterSeconds("Tue, 25 Aug 2020 10:00:30 GMT")).toBeUndefined();
  });
});
