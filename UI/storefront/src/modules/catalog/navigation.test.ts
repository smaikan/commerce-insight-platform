import { beforeEach, describe, expect, it, vi } from "vitest";

const apiGetMock = vi.hoisted(() => vi.fn());

vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiGet: apiGetMock }));

import { getStorefrontNavigation } from "./navigation";

describe("storefront navigation data", () => {
  beforeEach(() => {
    apiGetMock.mockReset();
    apiGetMock.mockImplementation(async (path: string) => {
      if (path.endsWith("product-types")) {
        return [
          { id: "type-1", name: "Yüzük", productCount: 8 },
          { id: "type-empty", name: "Boş Tür", productCount: 0 },
        ];
      }
      if (path.endsWith("collections")) return [{ id: "collection-1", name: "Takı Bakımı", productCount: 4 }];
      if (path.endsWith("brands")) return [{ id: "brand-1", name: "SERANTIS", productCount: 6 }];
      return [];
    });
  });

  // Burada üç facet isteğinin paralel modelden doğru ve sıfır sonuç içermeyen navigasyon hedeflerine dönüştüğünü doğruluyorum.
  it("maps facet responses to canonical navigation links", async () => {
    const groups = await getStorefrontNavigation();

    expect(apiGetMock).toHaveBeenCalledTimes(3);
    expect(groups).toEqual([
      {
        id: "categories",
        label: "Kategoriler",
        items: [{ id: "type-1", label: "Yüzük", href: "/category/yuzuk", productCount: 8 }],
      },
      {
        id: "collections",
        label: "Koleksiyonlar",
        href: "/collections",
        items: [{ id: "collection-1", label: "Takı Bakımı", href: "/collection/taki-bakimi", productCount: 4 }],
      },
      {
        id: "brands",
        label: "Markalar",
        items: [{ id: "brand-1", label: "SERANTIS", href: "/brand/serantis", productCount: 6 }],
      },
    ]);
  });
});
