import { describe, expect, it } from "vitest";

import {
  canCreateOrderReturnRequest,
  canCustomerCancelOrder,
  canOpenOrderReturnCenter,
} from "./lifecycle";

describe("order lifecycle presentation", () => {
  // Burada Shipped öncesindeki dört sipariş durumunda iptalin göründüğünü ve kargoya çıkınca kapandığını doğruluyorum.
  it("offers cancellation before shipped and removes it at shipped", () => {
    expect([0, 1, 2, 3].filter(canCustomerCancelOrder)).toEqual([0, 1, 2, 3]);
    expect([4, 5, 6, 7, 8, 9].some(canCustomerCancelOrder)).toBe(false);
    expect(canOpenOrderReturnCenter(3)).toBe(false);
    expect(canOpenOrderReturnCenter(4)).toBe(true);
  });

  // Burada Shipped aşamasında satış sonrası merkezin açıldığını fakat talep formunun teslimata kadar kapalı kaldığını doğruluyorum.
  it("opens the return center at shipped and the request form at delivered", () => {
    expect(canOpenOrderReturnCenter(4)).toBe(true);
    expect(canCreateOrderReturnRequest(4)).toBe(false);
    expect(canCreateOrderReturnRequest(5)).toBe(true);
    expect(canCreateOrderReturnRequest(7)).toBe(true);
    expect(canCreateOrderReturnRequest(8)).toBe(true);
    expect(canCreateOrderReturnRequest(9)).toBe(true);
  });

  // Burada iptal edilmiş siparişin iade merkezine yönlendirilmediğini doğruluyorum.
  it("keeps cancelled orders outside the return flow", () => {
    expect(canOpenOrderReturnCenter(6)).toBe(false);
  });
});
