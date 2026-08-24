import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { ScrollToTopButton } from "./scroll-to-top-button";

describe("ScrollToTopButton", () => {
  it("renders with proper accessibility attributes and icon", () => {
    const html = renderToStaticMarkup(<ScrollToTopButton />);
    expect(html).toContain('aria-label="Sayfanın başına dön"');
    expect(html).toContain("Başa Dön");
  });
});
