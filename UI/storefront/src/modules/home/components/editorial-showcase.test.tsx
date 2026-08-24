import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { BrandStory, EditorialPair } from "@/modules/home/components/editorial-showcase";

describe("homepage editorial showcase", () => {
  it("renders two crawlable showcase links with authoritative product counts", () => {
    const html = renderToStaticMarkup(
      <EditorialPair
        id="featured-categories"
        eyebrow="Kategoriler"
        title="Öne çıkan kategoriler"
        description="Açıklama"
        allHref="/categories"
        allLabel="Tüm kategoriler"
        compactMediaCorners
        items={[
          { id: "1", name: "Bileklik", href: "/category/bileklik", imageUrl: "https://cdn.example.com/bracelet.webp", imageAlt: "Bileklik", productCount: 12 },
          { id: "2", name: "Gözlük", href: "/category/gozluk", imageUrl: null, imageAlt: "Gözlük", productCount: 8 },
        ]}
      />,
    );

    expect(html).toContain('href="/category/bileklik"');
    expect(html).toContain('href="/category/gozluk"');
    expect(html).toContain("12 ürün");
    expect(html).toContain("8 ürün");
    expect(html).toContain('loading="lazy"');
    expect(html).toContain("home-shell");
    expect(html).toContain("rounded-lg");
  });

  it("uses only an image item from Alt Banner 1 and omits unsupported media", () => {
    const imageHtml = renderToStaticMarkup(<BrandStory image={{ id: "1", name: "Atölye", key: "story", mediaUrl: "https://cdn.example.com/story.webp", mediaType: 1, targetUrl: null, altText: "Özenle seçilmiş aksesuarlar", displayOrder: 0, isActive: true, isMain: false }} />);
    const videoHtml = renderToStaticMarkup(<BrandStory image={{ id: "2", name: "Video", key: "video", mediaUrl: "https://cdn.example.com/story.mp4", mediaType: 2, targetUrl: null, altText: null, displayOrder: 0, isActive: true, isMain: false }} />);

    expect(imageHtml).toContain("Kalite, ayrıntılarda kendini gösterir");
    expect(imageHtml).toContain("Özenle seçilmiş aksesuarlar");
    expect(videoHtml).toBe("");
  });
});
