import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { BANK_CARD_PROGRAMS, ProductInstallmentTable } from "./product-installment-table";

describe("product installment table", () => {
  // Burada tablo başlıklarının, banka kartı programlarının ve taksit seçeneklerinin erişilebilir tablo yapısında sunulduğunu doğruluyorum.
  it("renders the supported bank card programs and installment plans with semantic headers", () => {
    const html = renderToStaticMarkup(<ProductInstallmentTable price={1200} currency="TRY" />);

    expect(html).toContain("Taksit seçenekleri");
    expect(html).toContain("<details");
    expect(html).toContain("<summary");
    expect(html).toContain("Tek çekim");
    expect(html).toContain("2 taksit");
    expect(html).toContain("3 taksit");
    expect(html).toContain("6 taksit");
    expect(html).toContain("9 taksit");
    expect(html).toContain("12 taksit");
    expect(html).toContain('scope="col"');
    expect(html).toContain('scope="row"');

    // Tüm 8 banka kart ailesinin sekmelerde yer aldığını doğrula
    BANK_CARD_PROGRAMS.forEach((program) => {
      expect(html).toContain(program.name);
    });
  });

  // Burada aylık ve toplam tutarların yaklaşık olarak hesaplandığını ve iyzico güvenlik uyarısının yer aldığını doğruluyorum.
  it("labels calculated amounts as approximate and discloses final pricing with iyzico branding", () => {
    const html = renderToStaticMarkup(<ProductInstallmentTable price={1200} currency="TRY" />);

    expect(html).toContain("Aylık Tutar");
    expect(html).toContain("Toplam Tutar");
    expect(html).toContain("Vade Farksız");
    expect(html).toContain("uygulanabilecek vade farkı");
    expect(html).toContain("kesin tahsilat tutarı");
    expect(html).toContain("iyzico");
  });

  it("calculates accurate installment amounts for different price values", () => {
    const html1000 = renderToStaticMarkup(<ProductInstallmentTable price={1000} currency="TRY" />);
    expect(html1000).toContain("500,00"); // 2 taksit vade farksız 1000 / 2 = 500

    const html2000 = renderToStaticMarkup(<ProductInstallmentTable price={2000} currency="TRY" />);
    expect(html2000).toContain("1.000,00"); // 2 taksit vade farksız 2000 / 2 = 1000
  });
});
