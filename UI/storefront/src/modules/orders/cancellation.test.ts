import { describe, expect, it } from "vitest";

import {
  cancellationConfirmationMessage,
  cancellationOperationMessage,
  cancellationPollingDelay,
  cancellationProblemMessage,
  isCancellationPending,
  type OrderCancellationOperation,
} from "@/modules/orders/cancellation";

const operation: OrderCancellationOperation = {
  operationId: "3470e031-3fc8-42af-9755-f0fcae2b06cb",
  orderId: "bb49d4c3-9752-4116-9179-657c8d6259b0",
  status: 2,
  reversalType: 1,
  createdAt: "2026-08-24T07:19:00Z",
  updatedAt: "2026-08-24T07:19:03Z",
  nextAttemptAt: "2026-08-24T07:20:03Z",
  pollingUrl: "/api/orders/bb49d4c3-9752-4116-9179-657c8d6259b0/cancellation",
};

describe("customer order cancellation presentation", () => {
  // Burada yalnız Requested, Processing ve ReconciliationPending durumlarının otomatik polling'e devam ettiğini doğruluyorum.
  it("separates active and terminal operation states", () => {
    expect(([0, 1, 2] as const).every(isCancellationPending)).toBe(true);
    expect(([3, 4, 5] as const).some(isCancellationPending)).toBe(false);
  });

  // Burada provider zamanının polling'i aşırı hızlı veya aşırı yavaş hale getiremediğini doğruluyorum.
  it("clamps polling delay to a controlled interval", () => {
    const now = Date.parse("2026-08-24T07:20:00Z");
    expect(cancellationPollingDelay("2026-08-24T07:19:00Z", now)).toBe(1_500);
    expect(cancellationPollingDelay("2026-08-24T07:20:04Z", now)).toBe(4_000);
    expect(cancellationPollingDelay("2026-08-24T08:20:00Z", now)).toBe(10_000);
    expect(cancellationPollingDelay("invalid", now)).toBe(3_000);
  });

  // Burada ödenmiş sipariş onayının iade süresini banka yansıma süresiyle karıştırmadığını doğruluyorum.
  it("explains paid reversal separately from an unpaid cancellation", () => {
    expect(cancellationConfirmationMessage(2)).toMatch(/iyzico.*iade.*bankanıza/i);
    expect(cancellationConfirmationMessage(0)).toMatch(/tahsilat yoksa.*rezervasyonu/i);
  });

  // Burada belgelenmiş 409 kodlarının genel concurrency mesajına düşmeden doğru kurtarma adımını verdiğini doğruluyorum.
  it("maps financial conflicts without suggesting a blind retry", () => {
    expect(cancellationProblemMessage({ status: 409, code: "payment_reversal_data_missing" })).toMatch(/işlem bilgileri eksik/i);
    expect(cancellationProblemMessage({ status: 409, code: "payment_reversal_rejected" })).toMatch(/kabul etmedi/i);
    expect(cancellationProblemMessage({ status: 409, code: "payment_reversal_manual_review" })).toMatch(/inceleme/i);
    expect(cancellationProblemMessage({ status: 409, code: "order_cancellation_not_allowed" })).toMatch(/kargoya verildiği/i);
  });

  // Burada ManualReview durumunun ikinci iade çağrısı yerine destek ve bekleme yönlendirmesi verdiğini doğruluyorum.
  it("keeps manual review terminal in the UI", () => {
    expect(cancellationOperationMessage({ ...operation, status: 5 })).toMatch(/ikinci bir iptal isteği göndermeyin/i);
    expect(cancellationOperationMessage(operation)).toMatch(/ikinci bir iade isteği gönderilmeyecek/i);
  });
});
