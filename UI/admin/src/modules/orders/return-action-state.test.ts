import { describe, expect, it, vi } from "vitest";
import { runReturnAction } from "./return-action-state";

describe("runReturnAction", () => {
  it("returns the authoritative return request and always closes pending", async () => {
    const pendingStates: boolean[] = [];
    const updatedReturn = {
      id: "22222222-2222-4222-8222-222222222222",
      orderId: "11111111-1111-4111-8111-111111111111",
      returnNumber: "RET-1001",
      type: 0 as const,
      status: 3 as const,
      refundTotal: 100,
      items: [],
      receivedAt: "2026-08-23T10:00:00Z",
      createdAt: "2026-08-22T14:30:00Z",
    };
    const action = vi.fn().mockResolvedValue({
      status: "success",
      message: "İade ürünleri teslim alındı.",
      returnRequest: updatedReturn,
    });

    const result = await runReturnAction(action, new FormData(), (pending) => pendingStates.push(pending));

    expect(result.returnRequest).toEqual(updatedReturn);
    expect(pendingStates).toEqual([true, false]);
  });

  it("closes pending and returns a safe error when the action rejects", async () => {
    const pendingStates: boolean[] = [];

    const result = await runReturnAction(
      vi.fn().mockRejectedValue(new Error("transport failure")),
      new FormData(),
      (pending) => pendingStates.push(pending),
    );

    expect(result).toEqual({
      status: "error",
      message: "İade talebi güncellenemedi. Bağlantıyı kontrol edip tekrar deneyin.",
    });
    expect(pendingStates).toEqual([true, false]);
  });
});
