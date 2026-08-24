import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";
import {
  CollectionSelector,
  MAX_PRODUCT_COLLECTIONS,
  updateCollectionSelection,
} from "./collection-selector";
import type { Collection } from "../types";

const collections: Collection[] = [
  {
    id: "11111111-1111-4111-8111-111111111111",
    name: "Yaz Koleksiyonu",
    url: "yaz-koleksiyonu",
    isActive: true,
    isFeatured: false,
    displayOrder: 0,
  },
  {
    id: "22222222-2222-4222-8222-222222222222",
    name: "Yeni Sezon",
    url: "yeni-sezon",
    isActive: true,
    isFeatured: false,
    displayOrder: 1,
  },
];

describe("collection selection", () => {
  it("adds and removes names while preventing Turkish case-insensitive duplicates", () => {
    const added = updateCollectionSelection([], "Yaz Koleksiyonu", true);
    expect(added).toMatchObject({ selected: ["Yaz Koleksiyonu"], changed: true });

    const duplicate = updateCollectionSelection(added.selected, "yaz koleksiyonu", true);
    expect(duplicate.changed).toBe(false);
    expect(duplicate.message).toContain("zaten");

    expect(updateCollectionSelection(added.selected, "YAZ KOLEKSİYONU", false))
      .toMatchObject({ selected: [], changed: true });
  });

  it("enforces the documented maximum collection count", () => {
    const selected = Array.from({ length: MAX_PRODUCT_COLLECTIONS }, (_, index) => `Koleksiyon ${index + 1}`);
    const result = updateCollectionSelection(selected, "Bir tane daha", true);
    expect(result.changed).toBe(false);
    expect(result.message).toContain(String(MAX_PRODUCT_COLLECTIONS));
  });

  it("renders a checklist, new-name input and selected-list guidance", () => {
    const html = renderToStaticMarkup(
      <CollectionSelector collections={collections} onCollectionsChange={vi.fn()} />,
    );

    expect(html).toContain("type=\"checkbox\"");
    expect(html).toContain("Yaz Koleksiyonu");
    expect(html).toContain("Yeni Sezon");
    expect(html).toContain("Yeni koleksiyon adı");
    expect(html).toContain("Koleksiyon ekle");
    expect(html).toContain("otomatik oluşturulur");
    expect(html).not.toContain("<select");
  });
});
