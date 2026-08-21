import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { ContactForm } from "@/modules/contact/components/contact-form";

describe("contact form semantics", () => {
  it("renders typed subject values, field constraints and the privacy notice", () => {
    const html = renderToStaticMarkup(<ContactForm turnstileRequired={false} turnstileSiteKey="" />);

    expect(html).toContain('name="subject"');
    expect(html).toContain('<option value="0"');
    expect(html).toContain('<option value="5"');
    expect(html).toContain('minLength="20"');
    expect(html).toContain('maxLength="5000"');
    expect(html).toContain('href="/privacy-policy"');
    expect(html).not.toContain("Mesajınız Bize Ulaştı");
  });

  it("fails closed when production challenge configuration is missing", () => {
    const html = renderToStaticMarkup(<ContactForm turnstileRequired turnstileSiteKey="" />);
    expect(html).toContain("Güvenlik doğrulaması şu anda başlatılamıyor");
    expect(html).toContain("disabled");
  });
});
