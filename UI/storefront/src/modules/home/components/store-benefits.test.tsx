import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { StoreBenefits } from "./store-benefits";

describe("store benefits", () => {
  // Burada footer öncesindeki dört bilgi bloğunun belgeli teslimat, destek, cayma ve ödeme hedeflerine bağlandığını doğruluyorum.
  it("renders four useful and reachable store information blocks", () => {
    const html = renderToStaticMarkup(<StoreBenefits />);

    expect(html.match(/<a /g)).toHaveLength(4);
    expect(html).toContain("TESLİMAT SEÇENEKLERİ");
    expect(html).toContain("DESTEK KANALLARI");
    expect(html).toContain("14 GÜN İÇİNDE CAYMA");
    expect(html).toContain("GÜVENLİ ÖDEME");
    expect(html).toContain('href="/cancellation-and-refund"');
    expect(html).toContain("home-shell");
  });
});
