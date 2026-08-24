import { beforeEach, describe, expect, it, vi } from "vitest";

const { apiRequestMock } = vi.hoisted(() => ({ apiRequestMock: vi.fn() }));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiRequest: apiRequestMock }));

import { bulkUpdateProductVariants, createProductVariant, deleteProductVariant } from "./api";
import type { AdminSession } from "@/lib/auth/contracts";

const session = { accessToken: "test-admin-token" } as AdminSession;

describe("product variant API client", () => {
  beforeEach(() => {
    apiRequestMock.mockReset();
    apiRequestMock.mockResolvedValue([]);
  });

  it("sends existing variants to the atomic bulk PUT endpoint", async () => {
    const body = {
      variants: [{
        id: "11111111-1111-4111-8111-111111111111",
        name: "Uzunluk",
        value: "45 CM",
        sku: "SKU-B",
        price: 899.9,
        stock: 5,
        isActive: true,
        expectedConcurrencyToken: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      }],
    };

    await bulkUpdateProductVariants("P00004", body, session);

    expect(apiRequestMock).toHaveBeenCalledWith(
      "/api/product-variants/by-product/P00004/bulk",
      { method: "PUT", body, accessToken: session.accessToken },
    );
  });

  it("keeps new variants on the individual create endpoint", async () => {
    await createProductVariant("P00004", {
      name: "Uzunluk",
      value: "55 CM",
      sku: "SKU-C",
      price: 899.9,
      stock: 2,
      isActive: true,
    }, session);

    expect(apiRequestMock).toHaveBeenCalledWith(
      "/api/product-variants/by-product/P00004",
      {
        method: "POST",
        body: expect.objectContaining({ value: "55 CM", sku: "SKU-C" }),
        accessToken: session.accessToken,
      },
    );
  });

  it("deletes a persisted variant through the documented endpoint without a request body", async () => {
    apiRequestMock.mockResolvedValueOnce(undefined);

    await deleteProductVariant("11111111-1111-4111-8111-111111111111", session);

    expect(apiRequestMock).toHaveBeenCalledWith(
      "/api/product-variants/11111111-1111-4111-8111-111111111111",
      { method: "DELETE", accessToken: session.accessToken },
    );
  });
});
