import { describe, expect, it } from "vitest";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { availableReturnActions, isReturnActionAwaitingResult } from "./return-lifecycle";

const emptyTimestamps = {
  approvedAt: null,
  rejectedAt: null,
  receivedAt: null,
  completedAt: null,
};

describe("availableReturnActions", () => {
  it("offers only receipt for a new requested return", () => {
    expect(availableReturnActions({ status: 0, ...emptyTimestamps })).toEqual(["receive"]);
  });

  it("offers approval and rejection after a new return is received", () => {
    expect(availableReturnActions({
      status: 3,
      ...emptyTimestamps,
      receivedAt: "2026-08-23T10:00:00Z",
    })).toEqual(["reject", "approve"]);
  });

  it("keeps bounded receipt and completion actions for legacy records", () => {
    expect(availableReturnActions({
      status: 1,
      ...emptyTimestamps,
      approvedAt: "2026-08-20T09:00:00Z",
    })).toEqual(["receive"]);

    expect(availableReturnActions({
      status: 3,
      ...emptyTimestamps,
      approvedAt: "2026-08-20T09:00:00Z",
      receivedAt: "2026-08-21T10:00:00Z",
    })).toEqual(["complete"]);
  });

  it("offers no action for terminal or inconsistent records", () => {
    expect(availableReturnActions({
      status: 1,
      ...emptyTimestamps,
      approvedAt: "2026-08-23T10:05:00Z",
      receivedAt: "2026-08-23T10:00:00Z",
    })).toEqual([]);
    expect(availableReturnActions({ status: 2, ...emptyTimestamps, rejectedAt: "2026-08-23T10:05:00Z" })).toEqual([]);
    expect(availableReturnActions({ status: 4, ...emptyTimestamps, completedAt: "2026-08-23T10:05:00Z" })).toEqual([]);
    expect(availableReturnActions({ status: 3, ...emptyTimestamps })).toEqual([]);
  });
});

describe("isReturnActionAwaitingResult", () => {
  it("stops pending when the action returns even if refresh continues", () => {
    const returnedState: AdminMutationResult = { status: "success", message: "İade ürünleri teslim alındı." };

    expect(isReturnActionAwaitingResult(true, returnedState, null)).toBe(false);
    expect(isReturnActionAwaitingResult(true, null, null)).toBe(true);
  });
});
