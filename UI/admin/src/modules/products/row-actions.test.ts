import { describe, expect, it } from "vitest";
import { getQuickProductStatus } from "./row-actions";

describe("ürün satırı hızlı durum hedefi", () => {
  // Burada aktif ürünün taslağa, taslağın da aktife geçirildiğini doğruluyorum.
  it("toggles active and draft statuses", () => {
    expect(getQuickProductStatus(1)).toBe(0);
    expect(getQuickProductStatus(0)).toBe(1);
  });

  // Burada pasif ve arşivlenmiş ürünlerde belgelenmemiş hızlı geçiş göstermiyorum.
  it("does not infer transitions for passive and archived statuses", () => {
    expect(getQuickProductStatus(2)).toBeNull();
    expect(getQuickProductStatus(3)).toBeNull();
  });
});
