import { beforeEach, describe, expect, it, vi } from "vitest";

const {
  bulkUpdateProductVariantsMock,
  createProductVariantMock,
  createProductImageMock,
  deleteProductVariantMock,
  getProductMock,
  getProductImagesMock,
  parseProductFormMock,
  requireAdminActionSessionMock,
  updateProductMock,
  updateProductImageMock,
  patchProductStateMock,
} = vi.hoisted(() => ({
  bulkUpdateProductVariantsMock: vi.fn(),
  createProductVariantMock: vi.fn(),
  createProductImageMock: vi.fn(),
  deleteProductVariantMock: vi.fn(),
  getProductMock: vi.fn(),
  getProductImagesMock: vi.fn(),
  parseProductFormMock: vi.fn(),
  requireAdminActionSessionMock: vi.fn(),
  updateProductMock: vi.fn(),
  updateProductImageMock: vi.fn(),
  patchProductStateMock: vi.fn(),
}));

vi.mock("server-only", () => ({}));
vi.mock("next/cache", () => ({ revalidatePath: vi.fn() }));
vi.mock("@/lib/admin/mutation-error", () => ({ adminMutationError: vi.fn() }));
vi.mock("@/lib/api/problem", () => ({ ApiError: class ApiError extends Error {} }));
vi.mock("@/lib/auth/session", () => ({ requireAdminActionSession: requireAdminActionSessionMock }));
vi.mock("@/modules/products/action-error", () => ({
  productActionError: () => ({ status: "error", message: "Varyant kaydedilemedi." }),
  remapProductVariantBulkError: (error: unknown) => error,
}));
vi.mock("@/modules/products/api", () => ({
  bulkUpdateProductVariants: bulkUpdateProductVariantsMock,
  createProduct: vi.fn(),
  createProductImage: createProductImageMock,
  createProductVariant: createProductVariantMock,
  deleteProduct: vi.fn(),
  deleteProductImage: vi.fn(),
  deleteProductVariant: deleteProductVariantMock,
  getProduct: getProductMock,
  getProductImages: getProductImagesMock,
  patchProductState: patchProductStateMock,
  updateProduct: updateProductMock,
  updateProductImage: updateProductImageMock,
}));
vi.mock("@/modules/products/form-data", () => ({ parseProductForm: parseProductFormMock }));
vi.mock("@/modules/products/product-media", () => ({
  isTrustedCloudinaryProductAsset: vi.fn(),
  MAX_PRODUCT_IMAGES: 20,
}));

import { commitProductMediaAction, deleteProductVariantAction, updateProductAction } from "./actions";

describe("product update action", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireAdminActionSessionMock.mockResolvedValue({ accessToken: "test-admin-token" });
    bulkUpdateProductVariantsMock.mockResolvedValue([]);
    parseProductFormMock.mockReturnValue({
      ok: true,
      value: {
        productId: "P00004",
        base: {},
        baseChanged: false,
        status: 0,
        isFeatured: false,
        hasVariants: true,
        originalStatus: 0,
        originalIsFeatured: false,
        originalHasVariants: true,
        variants: [
          { name: "Renk", value: "Siyah", sku: "LUNA-3", price: 899.9, stock: 3, isActive: true },
          { name: "Renk", value: "Beyaz", sku: "LUNA-4", price: 899.9, stock: 3, isActive: true },
        ],
        variantFieldIndexes: [2, 3],
      },
    });
    getProductMock.mockImplementation(async () => {
      const parsed = parseProductFormMock.mock.results.at(-1)?.value;
      const value = parsed?.ok ? parsed.value : undefined;
      return {
        mainSku: "MAIN",
        hasVariants: value?.hasVariants ?? true,
        variants: (value?.variants || []).filter((variant: { id?: string }) => Boolean(variant.id)),
      };
    });
  });

  // Burada üçüncü varyant hata verse bile dördüncü varyant isteğinin gönderilip kaydedildiğini doğruluyorum.
  it("continues with the fourth variant when the third variant fails", async () => {
    createProductVariantMock
      .mockRejectedValueOnce(new Error("Variant SKU already exists."))
      .mockResolvedValueOnce(undefined);

    const result = await updateProductAction({ status: "idle" }, new FormData());

    expect(result.status).toBe("partial");
    expect(result.completedOperations).toContain("Renk: Beyaz yeni varyantı");
    expect(result.failedOperations).toContain("Renk: Siyah yeni varyantı");
    expect(createProductVariantMock).toHaveBeenCalledTimes(2);
    expect(createProductVariantMock.mock.calls[0][1]).toMatchObject({ sku: "LUNA-3", value: "Siyah", stock: 3 });
    expect(createProductVariantMock).toHaveBeenCalledWith(
      "P00004",
      expect.objectContaining({ sku: "LUNA-4", stock: 3 }),
      expect.anything(),
    );
    expect(updateProductMock).not.toHaveBeenCalled();
    expect(bulkUpdateProductVariantsMock).not.toHaveBeenCalled();
    expect(patchProductStateMock).not.toHaveBeenCalled();
  });

  // Burada yalnız medya taslağı kaydedilirken dokunulmamış varyantların ve gereksiz ürün GET'inin akışa girmediğini doğruluyorum.
  it("completes an image-only form intent without product or variant API operations", async () => {
    parseProductFormMock.mockReturnValue({
      ok: true,
      value: {
        productId: "P00004",
        base: {},
        baseChanged: false,
        status: 0,
        isFeatured: false,
        hasVariants: true,
        originalStatus: 0,
        originalIsFeatured: false,
        originalHasVariants: true,
        variants: [],
        variantFieldIndexes: [],
      },
    });

    const result = await updateProductAction({ status: "idle" }, new FormData());

    expect(result).toMatchObject({ status: "success", productId: "P00004" });
    expect(result.completionToken).toBeTruthy();
    expect(getProductMock).not.toHaveBeenCalled();
    expect(bulkUpdateProductVariantsMock).not.toHaveBeenCalled();
    expect(createProductVariantMock).not.toHaveBeenCalled();
    expect(updateProductMock).not.toHaveBeenCalled();
    expect(patchProductStateMock).not.toHaveBeenCalled();
  });

  // Burada varyantsız ürünü dönüştürürken varsayılan varyantın mod PATCH'inden önce,
  // yeni çapraz kombinasyonların ise mod açıldıktan sonra kaydedildiğini doğruluyorum.
  it("bulk-updates the existing default variant before enabling variant mode", async () => {
    parseProductFormMock.mockReturnValue({
      ok: true,
      value: {
        productId: "P00004",
        base: {},
        baseChanged: false,
        status: 0,
        isFeatured: false,
        hasVariants: true,
        originalStatus: 0,
        originalIsFeatured: false,
        originalHasVariants: false,
        variants: [
          { id: "variant-default", expectedConcurrencyToken: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", name: "Uzunluk", value: "40 CM", sku: "LUNA-1", price: 899.9, stock: 3, isActive: true },
          { name: "Uzunluk", value: "50 CM", sku: "LUNA-2", price: 899.9, stock: 3, isActive: true },
        ],
        variantFieldIndexes: [0, 1],
      },
    });

    const result = await updateProductAction({ status: "idle" }, new FormData());

    expect(result.status).toBe("success");
    expect(bulkUpdateProductVariantsMock).toHaveBeenCalledWith(
      "P00004",
      { variants: [expect.objectContaining({
        id: "variant-default",
        value: "40 CM",
        expectedConcurrencyToken: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      })] },
      expect.anything(),
    );
    expect(patchProductStateMock).toHaveBeenCalledWith(
      "P00004",
      "has-variants",
      { hasVariants: true },
      expect.anything(),
    );
    expect(createProductVariantMock).toHaveBeenCalledWith(
      "P00004",
      expect.objectContaining({ value: "50 CM" }),
      expect.anything(),
    );
    expect(bulkUpdateProductVariantsMock.mock.invocationCallOrder[0]).toBeLessThan(
      patchProductStateMock.mock.invocationCallOrder[0],
    );
    expect(patchProductStateMock.mock.invocationCallOrder[0]).toBeLessThan(
      createProductVariantMock.mock.invocationCallOrder[0],
    );
  });

  // Burada varyant dönüşümündeki tekil hatanın bağımsız ürün işlemlerini durdurmadığını doğruluyorum.
  it("continues product operations after a failing variant conversion", async () => {
    parseProductFormMock.mockReturnValue({
      ok: true,
      value: {
        productId: "P00004",
        base: { title: "Güncel başlık" },
        baseChanged: true,
        status: 1,
        isFeatured: true,
        hasVariants: true,
        originalStatus: 0,
        originalIsFeatured: false,
        originalHasVariants: false,
        variants: [
          { id: "variant-default", expectedConcurrencyToken: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", name: "Uzunluk", value: "40 CM", sku: "LUNA-1", price: 899.9, stock: 3, isActive: true },
        ],
        variantFieldIndexes: [0],
      },
    });
    bulkUpdateProductVariantsMock.mockRejectedValueOnce(new Error("Option link persistence failed."));

    const result = await updateProductAction({ status: "idle" }, new FormData());

    expect(result.status).toBe("partial");
    expect(bulkUpdateProductVariantsMock).toHaveBeenCalledTimes(1);
    expect(updateProductMock).toHaveBeenCalledTimes(1);
    expect(patchProductStateMock).toHaveBeenCalledWith(
      "P00004",
      "has-variants",
      { hasVariants: true },
      expect.anything(),
    );
    expect(patchProductStateMock).toHaveBeenCalledWith(
      "P00004",
      "status",
      { status: 1 },
      expect.anything(),
    );
    expect(patchProductStateMock).toHaveBeenCalledWith(
      "P00004",
      "featured",
      { isFeatured: true },
      expect.anything(),
    );
    expect(createProductVariantMock).not.toHaveBeenCalled();
  });

  // Burada iki mevcut SKU takasının ayrı PUT'lar yerine tek atomik bulk payload ile gönderildiğini doğruluyorum.
  it("sends an SKU swap in one atomic bulk request and keeps returned tokens", async () => {
    const firstUpdated = {
      id: "11111111-1111-1111-1111-111111111111",
      productId: "P00004",
      name: "Uzunluk",
      value: "45 CM",
      sku: "SKU-B",
      price: 899.9,
      netPrice: 749.9,
      stock: 5,
      addToCartCount: 0,
      purchaseCount: 0,
      isActive: true,
      concurrencyToken: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    };
    const secondUpdated = {
      ...firstUpdated,
      id: "22222222-2222-2222-2222-222222222222",
      value: "50 CM",
      sku: "SKU-A",
      concurrencyToken: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    };
    parseProductFormMock.mockReturnValue({
      ok: true,
      value: {
        productId: "P00004",
        base: {},
        baseChanged: false,
        status: 0,
        isFeatured: false,
        hasVariants: true,
        originalStatus: 0,
        originalIsFeatured: false,
        originalHasVariants: true,
        variants: [
          { id: firstUpdated.id, expectedConcurrencyToken: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", name: "Uzunluk", value: "45 CM", sku: "SKU-B", price: 899.9, stock: 5, isActive: true },
          { id: secondUpdated.id, expectedConcurrencyToken: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", name: "Uzunluk", value: "50 CM", sku: "SKU-A", price: 899.9, stock: 5, isActive: true },
        ],
        variantFieldIndexes: [2, 5],
      },
    });
    bulkUpdateProductVariantsMock.mockResolvedValueOnce([firstUpdated, secondUpdated]);
    getProductMock.mockResolvedValueOnce({
      mainSku: "MAIN",
      hasVariants: true,
      variants: [
        { ...firstUpdated, stock: 12, sku: "stale-a", concurrencyToken: "old-a" },
        { ...secondUpdated, stock: 14, sku: "stale-b", concurrencyToken: "old-b" },
      ],
    });

    const result = await updateProductAction({ status: "idle" }, new FormData());

    expect(result.status).toBe("success");
    expect(bulkUpdateProductVariantsMock).toHaveBeenCalledTimes(1);
    expect(bulkUpdateProductVariantsMock.mock.calls[0][1].variants).toEqual([
      expect.objectContaining({ id: firstUpdated.id, sku: "SKU-B", stock: 12, stockAdjustmentReason: null, expectedConcurrencyToken: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" }),
      expect.objectContaining({ id: secondUpdated.id, sku: "SKU-A", stock: 14, stockAdjustmentReason: null, expectedConcurrencyToken: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" }),
    ]);
    expect(result.savedVariantEditorState?.variants).toEqual([firstUpdated, secondUpdated]);
    expect(createProductVariantMock).not.toHaveBeenCalled();
  });
});

describe("product variant delete action", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireAdminActionSessionMock.mockResolvedValue({ accessToken: "test-admin-token" });
    deleteProductVariantMock.mockResolvedValue(undefined);
  });

  it("deletes the selected variant with the admin session", async () => {
    const result = await deleteProductVariantAction(
      "P00004",
      "11111111-1111-4111-8111-111111111111",
    );

    expect(deleteProductVariantMock).toHaveBeenCalledWith(
      "11111111-1111-4111-8111-111111111111",
      expect.objectContaining({ accessToken: "test-admin-token" }),
    );
    expect(result).toMatchObject({
      status: "success",
      message: "Varyant üründen kaldırıldı.",
      refresh: true,
    });
  });
});

describe("product media commit action", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireAdminActionSessionMock.mockResolvedValue({ accessToken: "test-admin-token" });
    createProductImageMock.mockResolvedValue({});
    updateProductImageMock.mockResolvedValue({});
  });

  it("persists the requested existing image order without sorting by image label", async () => {
    const first = {
      id: "11111111-1111-4111-8111-111111111111",
      productId: "P00004",
      imageUrl: "https://example.com/zebra.webp",
      altText: "Zebra",
      displayOrder: 0,
      isMain: true,
    };
    const second = {
      id: "22222222-2222-4222-8222-222222222222",
      productId: "P00004",
      imageUrl: "https://example.com/alpha.webp",
      altText: "Alpha",
      displayOrder: 1,
      isMain: false,
    };
    getProductImagesMock.mockResolvedValue({ items: [first, second], pageNumber: 1, pageSize: 100, totalCount: 2, totalPages: 1 });

    const result = await commitProductMediaAction({
      productId: "P00004",
      mainExistingImageId: first.id,
      existingImages: [
        { id: second.id, displayOrder: 0 },
        { id: first.id, displayOrder: 1 },
      ],
      newImages: [],
    });

    expect(result.status).toBe("success");
    expect(result.updatedExistingImageIds).toEqual([second.id, first.id]);
    expect(updateProductImageMock).toHaveBeenNthCalledWith(1, second.id, expect.objectContaining({ displayOrder: 0 }), expect.anything());
    expect(updateProductImageMock).toHaveBeenNthCalledWith(2, first.id, expect.objectContaining({ displayOrder: 1, isMain: true }), expect.anything());
    expect(createProductImageMock).not.toHaveBeenCalled();
  });
});
