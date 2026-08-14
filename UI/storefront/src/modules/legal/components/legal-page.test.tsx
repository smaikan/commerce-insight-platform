import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { LegalPage } from "./legal-page";

describe("legal page", () => {
  // Burada tek h1, bağlantılı içindekiler ve mantıksal bölüm başlıklarının ilk HTML içinde yer aldığını doğruluyorum.
  it("renders accessible document navigation and sections", () => {
    const html = renderToStaticMarkup(
      <LegalPage
        eyebrow="Yasal"
        title="Örnek Politika"
        summary="Örnek açıklama"
        sections={[
          { id: "first", title: "Birinci bölüm", content: <p>Birinci içerik</p> },
          { id: "second", title: "İkinci bölüm", content: <p>İkinci içerik</p> },
        ]}
      />,
    );

    expect(html.match(/<h1/g)).toHaveLength(1);
    expect(html).toContain('href="#first"');
    expect(html).toContain('id="first-title"');
    expect(html).toContain('aria-labelledby="second-title"');
    expect(html).toContain('id="main-content"');
  });
});
