import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

describe("hosted payment card boundary", () => {
  // Burada storefront checkout kaynağının PAN, CVV veya son kullanma tarihi toplayan alanlar içermediğini regresyon testiyle koruyorum.
  it("does not define card data fields", () => {
    const source = readFileSync(fileURLToPath(new URL("./checkout-form.tsx", import.meta.url)), "utf8");
    expect(source).not.toMatch(/name=["'](?:cardNumber|pan|cvv|cvc|expiry|expirationDate)["']/i);
    expect(source).not.toMatch(/autocomplete=["']cc-(?:number|csc|exp|exp-month|exp-year)["']/i);
    expect(source).toContain("Kart bilgileriniz iyzico’nun güvenli ödeme sayfasında alınır");
  });
});
