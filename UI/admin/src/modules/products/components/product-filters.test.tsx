import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { ProductFilters } from "./product-filters";
import { parseProductListQuery } from "../query";
import type { Brand, Collection, ProductType, Tag } from "../types";

const mockTypes: ProductType[] = [
  { id: "11111111-1111-4111-8111-111111111111", name: "Yüzük", isActive: true },
  { id: "22222222-2222-4222-8222-222222222222", name: "Kolye", isActive: true },
];

const mockBrands: Brand[] = [
  { id: "33333333-3333-4333-8333-333333333333", name: "Altınbaş", url: "altinbas", isActive: true },
];

const mockCollections: Collection[] = [
  {
    id: "44444444-4444-4444-8444-444444444444",
    name: "Yaz Koleksiyonu",
    url: "yaz-koleksiyonu",
    isActive: true,
    isFeatured: false,
    displayOrder: 0,
  },
];

const mockTags: Tag[] = [
  { id: "55555555-5555-4555-8555-555555555555", name: "trend", url: "trend", isActive: true },
];

describe("ProductFilters component", () => {
  // Burada filtre çubuğunun koleksiyon, etiket ve durum sekmeleri dahil tüm seçim alanlarını eksiksiz render ettiğini doğruluyorum.
  it("renders status tabs, collection and tag filter selects with options", () => {
    const query = parseProductListQuery({});
    const html = renderToStaticMarkup(
      <ProductFilters
        query={query}
        productTypes={mockTypes}
        brands={mockBrands}
        collections={mockCollections}
        tags={mockTags}
      />,
    );

    // Durum sekmeleri
    expect(html).toContain("Tümü");
    expect(html).toContain("Aktif");
    expect(html).toContain("Taslak");
    expect(html).toContain("Pasif");
    expect(html).toContain("Arşivlenmiş");
    expect(html).toContain("Öne Çıkanlar");

    // Koleksiyon seçici
    expect(html).toContain('name="collectionId"');
    expect(html).toContain("Tüm Koleksiyonlar");
    expect(html).toContain("Yaz Koleksiyonu");

    // Etiket seçici
    expect(html).toContain('name="tagId"');
    expect(html).toContain("Tüm Etiketler");
    expect(html).toContain("#trend");

    // Tip ve marka seçiciler
    expect(html).toContain('name="typeId"');
    expect(html).toContain("Yüzük");
    expect(html).toContain("Kolye");
    expect(html).toContain('name="brandId"');
    expect(html).toContain("Altınbaş");

    // Arama ve sıralama
    expect(html).toContain('name="search"');
    expect(html).toContain('name="sort"');
    expect(html).toContain('name="pageSize"');
  });

  // Burada aktif filtreler seçildiğinde ilgili kaldırma çiplerinin görüntülendiğini doğruluyorum.
  it("renders active filter chips when filters are applied", () => {
    const query = parseProductListQuery({
      search: "altın",
      collectionId: "44444444-4444-4444-8444-444444444444",
      tagId: "55555555-5555-4555-8555-555555555555",
      status: "1",
    });

    const html = renderToStaticMarkup(
      <ProductFilters
        query={query}
        productTypes={mockTypes}
        brands={mockBrands}
        collections={mockCollections}
        tags={mockTags}
      />,
    );

    expect(html).toContain("Aktif:");
    expect(html).toContain('Arama: &quot;altın&quot;');
    expect(html).toContain("Koleksiyon: Yaz Koleksiyonu");
    expect(html).toContain("Etiket: #trend");
    expect(html).toContain("Durum: Aktif");
    expect(html).toContain("Tümünü Temizle");
  });
});
