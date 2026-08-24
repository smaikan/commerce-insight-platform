import { describe, expect, it } from "vitest";
import { ApiError } from "../../lib/api/problem";
import { productActionError, remapProductVariantBulkError } from "./action-error";

describe("product action error", () => {
  it("maps a variant SKU conflict to the exact SKU field without calling it concurrency", () => {
    const result = productActionError(
      new ApiError({
        title: "Conflict",
        status: 409,
        code: "conflict",
        detail: "Product variant SKU already exists.",
        traceId: "safe-trace",
      }),
      {
        productId: "P00004",
        completedOperations: [],
        failedOperation: "Renk / Beden: Kahverengi / L yeni varyantı",
        conflictMessage: "Bu SKU başka bir varyantta kullanılıyor. Her varyant için benzersiz bir SKU girin.",
        conflictField: "variants.7.sku",
      },
    );

    expect(result.message).toContain("Kahverengi / L");
    expect(result.message).toContain("SKU başka bir varyantta kullanılıyor");
    expect(result.message).not.toContain("başka bir işlem tarafından değiştirildi");
    expect(result.fieldErrors?.["variants.7.sku"]).toBeDefined();
    expect(result.reloadHref).toBeUndefined();
  });

  it("keeps authoritative reload guidance only for concurrency conflicts", () => {
    const result = productActionError(
      new ApiError({
        title: "Concurrency conflict",
        status: 409,
        code: "concurrency_conflict",
      }),
      {
        productId: "P00004",
        completedOperations: [],
        failedOperation: "Renk: Kırmızı varyantı",
        currentRecordAvailable: true,
      },
    );

    expect(result.message).toContain("başka bir işlem tarafından değiştirildi");
    expect(result.reloadHref).toBe("/products/P00004?reload=1");
  });

  it("maps bulk SKU errors to the original visible variant rows", () => {
    const remapped = remapProductVariantBulkError(
      new ApiError({
        title: "Product variant SKU conflict",
        status: 409,
        code: "product_variant_sku_conflict",
        errors: {
          "variants[0].sku": ["SKU kullanımda."],
          "variants[1].sku": ["Başka bir SKU kullanımda."],
        },
      }),
      [2, 5],
    );

    const result = productActionError(remapped, {
      productId: "P00004",
      completedOperations: [],
      failedOperation: "varyantların toplu kaydı",
    });

    expect(result.fieldErrors).toEqual({
      "variants.2.sku": ["SKU kullanımda."],
      "variants.5.sku": ["Başka bir SKU kullanımda."],
    });
    expect(result.message).not.toContain("başka bir işlem tarafından değiştirildi");
  });
});
