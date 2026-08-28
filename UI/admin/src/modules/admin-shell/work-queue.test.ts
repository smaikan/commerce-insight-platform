import { describe, expect, it } from "vitest";
import {
  formatWorkQueueCount,
  getWorkQueueAccessibleLabel,
  getWorkQueueCount,
  isAdminWorkQueueSummary,
} from "./work-queue";

const summary = {
  ordersAwaitingProcessingCount: 7,
  newContactMessageCount: 2,
  generatedAtUtc: "2026-08-27T10:00:00Z",
};

describe("admin work queue", () => {
  // Burada BFF yanıtında negatif, kesirli veya geçersiz tarihli sayaçların reddedildiğini doğruluyorum.
  it("validates the runtime response before showing counts", () => {
    expect(isAdminWorkQueueSummary(summary)).toBe(true);
    expect(isAdminWorkQueueSummary({ ...summary, newContactMessageCount: -1 })).toBe(false);
    expect(isAdminWorkQueueSummary({ ...summary, ordersAwaitingProcessingCount: 1.5 })).toBe(false);
    expect(isAdminWorkQueueSummary({ ...summary, generatedAtUtc: "invalid" })).toBe(false);
  });

  // Burada her menü öğesinin yalnız kendi operasyon sayacını kullandığını doğruluyorum.
  it("maps order and contact counts independently", () => {
    expect(getWorkQueueCount(summary, "orders")).toBe(7);
    expect(getWorkQueueCount(summary, "contactMessages")).toBe(2);
    expect(getWorkQueueCount(null, "orders")).toBe(0);
    expect(getWorkQueueCount(summary, undefined)).toBe(0);
  });

  // Burada rozet sınırını ve ekran okuyucu açıklamalarını iş anlamıyla birlikte koruyorum.
  it("formats compact badges with accessible labels", () => {
    expect(formatWorkQueueCount(99)).toBe("99");
    expect(formatWorkQueueCount(100)).toBe("99+");
    expect(getWorkQueueAccessibleLabel("Siparişler", "orders", 7)).toBe("Siparişler, 7 işlem bekleyen sipariş");
    expect(getWorkQueueAccessibleLabel("İletişim Mesajları", "contactMessages", 2)).toBe("İletişim Mesajları, 2 yeni iletişim mesajı");
    expect(getWorkQueueAccessibleLabel("Siparişler", "orders", 0)).toBeUndefined();
  });
});
