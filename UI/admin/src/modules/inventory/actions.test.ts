import { beforeEach, describe, expect, it, vi } from "vitest";

const {
  createBulkStockMovementsMock,
  parseBulkMovementsMock,
  getProductMock,
  requireAdminActionSessionMock,
} = vi.hoisted(() => ({
  createBulkStockMovementsMock: vi.fn(),
  parseBulkMovementsMock: vi.fn(),
  getProductMock: vi.fn(),
  requireAdminActionSessionMock: vi.fn(),
}));

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

  it("returns the completed result for immediate embedded-form reconciliation", async () => {
    const result = await createProductStockMovementsAction(
      "P00042",
      { status: "idle" },
      new FormData(),
    );

    expect(createBulkStockMovementsMock).toHaveBeenCalledWith(
      [movement],
      expect.objectContaining({ accessToken: "test-admin-token" }),
    );
    expect(result).toMatchObject({ status: "success", movementCount: 1 });
  });

  it("keeps the standalone movement action independent from product validation", async () => {
    await createBulkStockMovementsAction({ status: "idle" }, new FormData());

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
