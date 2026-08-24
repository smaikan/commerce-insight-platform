import { describe, expect, it } from "vitest";
import { buildCostingHref, parseCostingQuery } from "./query";

// Burada maliyet URL'sinin bilinmeyen ve geçersiz varyant değerlerini taşımadığını doğruluyorum.
describe("costing query", () => {
  it("keeps a valid selection and bounded search", () => {
    const query = parseCostingQuery({ search: "  fixture  ", productVariantId: "77777777-7777-4777-8777-777777777777", ignored: "x" });
    expect(query).toEqual({ search: "fixture", productVariantId: "77777777-7777-4777-8777-777777777777" });
    expect(buildCostingHref(query)).toBe("/accounting/costing?search=fixture&productVariantId=77777777-7777-4777-8777-777777777777");
  });

  it("drops invalid identifiers", () => expect(parseCostingQuery({ productVariantId: "not-an-id" }).productVariantId).toBeNull());
});
