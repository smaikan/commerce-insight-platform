import { describe, expect, it } from "vitest";

import { cartErrorMessage, isConflictProblem } from "./cart-api";

describe("cart problem ayrımı", () => {
  // Burada yalnız API'nin concurrency kodunu yeniden yükleme gerektiren çakışma olarak kabul ettiğimi doğruluyorum.
  it("concurrency conflict hatasını tanır", () => {
    const problem = { status: 409, title: "Concurrency conflict", code: "concurrency_conflict" };

    expect(isConflictProblem(problem)).toBe(true);
    expect(cartErrorMessage(problem)).toContain("başka bir işlemde güncellendi");
  });

  // Burada stok kaynaklı 409 cevabının kullanıcıya yanlış concurrency mesajı göstermediğini doğruluyorum.
  it("stok conflict hatasını concurrency olarak sınıflandırmaz", () => {
    const problem = { status: 409, title: "Conflict", code: "conflict" };

    expect(isConflictProblem(problem)).toBe(false);
    expect(cartErrorMessage(problem)).toContain("stok veya satış durumu değişti");
  });
});
