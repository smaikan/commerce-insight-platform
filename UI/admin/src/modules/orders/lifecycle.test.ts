import { describe, expect, it } from "vitest";
import { isManagedOrderStatus, orderStatusTransitions } from "./lifecycle";

// Burada genel sipariş durumu seçeneklerinin iade ve refund akışlarını hiçbir durumda sunmadığını doğruluyorum.
describe("orderStatusTransitions", () => {
  it("hazırlanan siparişte yalnız kargoya verme ve iptal seçeneklerini döndürür", () => {
    expect(orderStatusTransitions(3).map((transition) => transition.value)).toEqual([4, 6]);
  });

  it("iade durumlarında genel geçiş sunmaz", () => {
    expect(orderStatusTransitions(7)).toEqual([]);
    expect(orderStatusTransitions(8)).toEqual([]);
    expect(orderStatusTransitions(9)).toEqual([]);
  });

  it("refund ve iade enumlarını genel durum allowlist'ine almaz", () => {
    expect(isManagedOrderStatus(6)).toBe(true);
    expect(isManagedOrderStatus(7)).toBe(false);
    expect(isManagedOrderStatus(8)).toBe(false);
    expect(isManagedOrderStatus(9)).toBe(false);
  });
});
