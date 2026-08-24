import { describe, expect, it } from "vitest";

import { selectMostPopulated } from "@/modules/catalog/showcase-ranking";

describe("homepage showcase ranking", () => {
  it("selects positive-count entries by product count with a stable Turkish-name tie break", () => {
    const result = selectMostPopulated([
      { id: "4", name: "Yüzük", productCount: 5 },
      { id: "2", name: "Çanta", productCount: 8 },
      { id: "3", name: "Bileklik", productCount: 5 },
      { id: "1", name: "Boş", productCount: 0 },
    ], 3);

    expect(result.map((item) => item.name)).toEqual(["Çanta", "Bileklik", "Yüzük"]);
  });

  it("returns an empty result for an invalid limit without mutating the input", () => {
    const items = [{ id: "1", name: "Kolye", productCount: 3 }];
    expect(selectMostPopulated(items, 0)).toEqual([]);
    expect(items).toEqual([{ id: "1", name: "Kolye", productCount: 3 }]);
  });
});
