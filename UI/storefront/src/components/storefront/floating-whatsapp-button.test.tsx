import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { FloatingWhatsappButton } from "./floating-whatsapp-button";

describe("FloatingWhatsappButton", () => {
  it("renders with correct target, rel, and WhatsApp link", () => {
    const html = renderToStaticMarkup(<FloatingWhatsappButton href="https://wa.me/905550000000" />);
    expect(html).toContain('href="https://wa.me/905550000000"');
    expect(html).toContain('target="_blank"');
    expect(html).toContain('rel="noreferrer"');
    expect(html).toContain("WhatsApp Destek");
  });
});
