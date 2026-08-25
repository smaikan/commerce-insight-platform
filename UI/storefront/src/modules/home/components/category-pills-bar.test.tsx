import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { CategoryPillsBar } from "./category-pills-bar";

describe("CategoryPillsBar", () => {
  it("renders category circles with links and names", () => {
    const html = renderToStaticMarkup(
      <CategoryPillsBar
        categories={[
          {
            id: "1",
            name: "Bileklik",
            href: "/category/bileklik",
            imageAlt: "Bileklik",
            imageUrl: "https://res.cloudinary.com/test/image.webp",
            productCount: 5,
          },
          {
            id: "2",
            name: "Zarif Kolyeler",
            href: "/category/kolye",
            imageAlt: "Zarif Kolyeler",
            imageUrl: null,
            productCount: 4,
          },
        ]}
      />
    );

    expect(html).toContain("Kategorilere Göre Keşfet");
    expect(html).toContain('href="/category/bileklik"');
    expect(html).toContain('href="/category/kolye"');
    expect(html).toContain("Bileklik");
    expect(html).toContain("Zarif Kolyeler");
    expect(html).toContain("whitespace-normal");
    expect(html).not.toContain("line-clamp-1");
  });

  it("returns null when categories array is empty", () => {
    const html = renderToStaticMarkup(<CategoryPillsBar categories={[]} />);
    expect(html).toBe("");
  });
});
