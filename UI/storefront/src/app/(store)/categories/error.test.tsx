import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import CategoriesError from "./error";

describe("categories error boundary", () => {
  // Burada API hatasının teknik ayrıntı sızdırmadan kontrollü bir uyarı ve yeniden deneme eylemi sunduğunu doğruluyorum.
  it("renders a safe retry state", () => {
    const html = renderToStaticMarkup(
      <CategoriesError error={new Error("secret upstream detail")} reset={vi.fn()} />,
    );

    expect(html).toContain('role="alert"');
    expect(html).toContain("Kategoriler şu anda yüklenemedi");
    expect(html).toContain("Tekrar dene");
    expect(html).not.toContain("secret upstream detail");
  });
});
