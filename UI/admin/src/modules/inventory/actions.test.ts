import { beforeEach, describe, expect, it, vi } from "vitest";

const {
  createBulkStockMovementsMock,
  parseBulkMovementsMock,
  getProductMock,
  requireAdminActionSessionMock,
  revalidatePathMock,
} = vi.hoisted(() => ({
  createBulkStockMovementsMock: vi.fn(),
  parseBulkMovementsMock: vi.fn(),
  getProductMock: vi.fn(),
  requireAdminActionSessionMock: vi.fn(),
  revalidatePathMock: vi.fn(),
}));

vi.mock("next/cache", () => ({ revalidatePath: revalidatePathMock }));
vi.mock("@/lib/api/problem", () => ({ ApiError: class ApiError extends Error {} }));
vi.mock("@/lib/auth/session", () => ({ requireAdminActionSession: requireAdminActionSessionMock }));
vi.mock("@/modules/inventory/api", () => ({ createBulkStockMovements: createBulkStockMovementsMock }));
vi.mock("@/modules/inventory/stock-movement-form-data", () => ({ parseBulkMovements: parseBulkMovementsMock }));
vi.mock("@/modules/products/api", () => ({ getProduct: getProductMock }));

import {
  createBulkStockMovementsAction,
  createProductStockMovementsAction,
} from "./actions";

const movement = {
  productVariantSku: "SKU-RED-M",
  type: 10,
  quantityDelta: 5,
  reason: "Ürün düzenleme ekranı",
};

describe("inventory actions", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireAdminActionSessionMock.mockResolvedValue({ accessToken: "test-admin-token" });
    parseBulkMovementsMock.mockReturnValue({ ok: true, movements: [movement] });
    getProductMock.mockResolvedValue({ variants: [{ sku: movement.productVariantSku }] });
    createBulkStockMovementsMock.mockResolvedValue({ movementCount: 1 });
  });

  it("refreshes the product detail after an embedded stock movement", async () => {
    const result = await createProductStockMovementsAction(
      "P00042",
      { status: "idle" },
      new FormData(),
    );

    expect(createBulkStockMovementsMock).toHaveBeenCalledWith(
      [movement],
      expect.objectContaining({ accessToken: "test-admin-token" }),
    );
    expect(revalidatePathMock).toHaveBeenCalledWith("/inventory/stock-movements");
    expect(revalidatePathMock).toHaveBeenCalledWith("/products/P00042");
    expect(result).toMatchObject({ status: "success", movementCount: 1 });
    expect(result.completionToken).toEqual(expect.any(String));
  });

  it("keeps the standalone movement action scoped to the inventory ledger", async () => {
    await createBulkStockMovementsAction({ status: "idle" }, new FormData());

    expect(revalidatePathMock).toHaveBeenCalledTimes(1);
    expect(revalidatePathMock).toHaveBeenCalledWith("/inventory/stock-movements");
    expect(getProductMock).not.toHaveBeenCalled();
  });

  it("rejects a SKU that does not belong to the product context", async () => {
    getProductMock.mockResolvedValueOnce({ variants: [{ sku: "ANOTHER-PRODUCT-SKU" }] });

    const result = await createProductStockMovementsAction(
      "P00042",
      { status: "idle" },
      new FormData(),
    );

    expect(result.status).toBe("error");
    expect(result.fieldErrors?.["movements[0].productVariantSku"]).toBeDefined();
    expect(createBulkStockMovementsMock).not.toHaveBeenCalled();
  });
});
