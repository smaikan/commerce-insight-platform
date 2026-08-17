import { beforeEach, describe, expect, it, vi } from "vitest";

const { apiGetMock } = vi.hoisted(() => ({ apiGetMock: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiGet: apiGetMock }));

import { getPublishedProductFacets } from "@/modules/catalog/api";

describe("published catalog facets", () => {
  beforeEach(() => { apiGetMock.mockReset(); apiGetMock.mockResolvedValue([]); });

  // Burada üç facet ucunun aynı aktif filtre bağlamını alarak self-exclusion hesabını API'ye bıraktığını doğruluyorum.
  it("passes the same selected filters to all published facet endpoints", async () => {
    await getPublishedProductFacets({ BrandId: "brand", CollectionId: "collection", TypeId: "type" });
    expect(apiGetMock).toHaveBeenCalledTimes(3);
    for (const [path] of apiGetMock.mock.calls) {
      expect(path).toContain("BrandId=brand");
      expect(path).toContain("CollectionId=collection");
      expect(path).toContain("TypeId=type");
      expect(path).toContain("/api/products/published/facets/");
    }
  });
});
