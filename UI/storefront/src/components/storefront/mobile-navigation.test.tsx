import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { MobileNavigation } from "./mobile-navigation";

describe("mobile navigation", () => {
  // Burada kapalı hamburger menüsünün erişilebilir adını, durumunu ve kontrol ettiği panel bağlantısını ilk HTML'de doğruluyorum.
  it("renders an accessible collapsed trigger", () => {
    const html = renderToStaticMarkup(<MobileNavigation currency="TRY" siteName="SERANTIS" />);

    expect(html).toContain('aria-label="Menüyü aç"');
    expect(html).toContain('aria-expanded="false"');
    expect(html).toContain('aria-controls="mobile-navigation-panel"');
    expect(html).toContain('id="mobile-navigation-panel"');
    expect(html).toContain("<dialog");
    expect(html).toContain('aria-label="Navigasyon menüsü"');
    expect(html).not.toContain("<details");
  });
});
