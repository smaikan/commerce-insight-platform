import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn() }),
}));

import { ActivePaymentRecoveryDialog } from "@/modules/checkout/components/active-payment-recovery-dialog";

describe("active payment recovery dialog", () => {
  // Burada ödemeden geri dönen müşteriye yeni sipariş yerine aynı ödeme ve güvenli iptal kararlarının sunulduğunu doğruluyorum.
  it("renders the two authoritative recovery actions", () => {
    const html = renderToStaticMarkup(
      <ActivePaymentRecoveryDialog
        orderId="bb49d4c3-9752-4116-9179-657c8d6259b0"
        orderNumber="ORD-RECOVERY-001"
        orderStatus={0}
        accessMode="guest"
        onCancelled={vi.fn()}
      />,
    );

    expect(html).toContain("<dialog");
    expect(html).toContain('aria-labelledby="payment-recovery-title"');
    expect(html).toContain("Ödemeniz henüz tamamlanmadı");
    expect(html).toContain("ORD-RECOVERY-001");
    expect(html).toContain("Ödemeye devam et");
    expect(html).toContain("Siparişi iptal et");
    expect(html).toContain("ayrılan stok yeniden kullanılabilir olur");
    expect(html).not.toContain("Alışverişe devam et");
  });
});
