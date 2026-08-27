import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { SandboxPaymentNotice } from "@/modules/checkout/components/sandbox-payment-notice";

describe("sandbox payment notice", () => {
  // Burada test kartının form alanı yerine açıklamalı ve klavye ile açılabilen bir bilgi yüzeyinde sunulduğunu doğruluyorum.
  it("renders selectable card information without collecting card data", () => {
    const html = renderToStaticMarkup(<SandboxPaymentNotice cardNumber="4543590000000006" />);

    expect(html).toContain("Test ödeme ortamı");
    expect(html).toContain("Gerçek kart kullanmayın");
    expect(html).toContain("4543 5900 0000 0006");
    expect(html).toContain("herhangi bir test adı");
    expect(html).toContain("AA/YY");
    expect(html).toContain("3 haneli sayı");
    expect(html).toContain("<details");
    expect(html).toContain("<summary");
    expect(html).toContain('type="button"');
    expect(html).not.toContain('name="cardNumber"');
    expect(html).not.toContain("<input");
  });
});
