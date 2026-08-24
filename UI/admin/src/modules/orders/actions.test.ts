import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ReturnRequest } from "@/modules/orders/types";

const {
  advanceReturnRequestMock,
  ApiErrorMock,
  availableReturnActionsMock,
  decideReturnRequestMock,
  getReturnRequestMock,
  isManagedOrderStatusMock,
  requireAdminActionSessionMock,
  revalidatePathMock,
  updateOrderStatusMock,
} = vi.hoisted(() => ({
  advanceReturnRequestMock: vi.fn(),
  ApiErrorMock: class ApiError extends Error {
    problem: { title: string; status: number; code?: string; traceId?: string; detail?: string };

    constructor(problem: { title: string; status: number; code?: string; traceId?: string; detail?: string }) {
      super(problem.detail || problem.title);
      this.problem = problem;
    }
  },
  availableReturnActionsMock: vi.fn(),
  decideReturnRequestMock: vi.fn(),
  getReturnRequestMock: vi.fn(),
  isManagedOrderStatusMock: vi.fn(),
  requireAdminActionSessionMock: vi.fn(),
  revalidatePathMock: vi.fn(),
  updateOrderStatusMock: vi.fn(),
}));

vi.mock("next/cache", () => ({ revalidatePath: revalidatePathMock }));
vi.mock("@/lib/admin/mutation-error", () => ({
  adminMutationError: (_error: unknown, fallback: string) => ({ status: "error", message: fallback }),
}));
vi.mock("@/lib/api/problem", () => ({ ApiError: ApiErrorMock }));
vi.mock("@/lib/auth/session", () => ({ requireAdminActionSession: requireAdminActionSessionMock }));
vi.mock("@/modules/orders/api", () => ({
  advanceReturnRequest: advanceReturnRequestMock,
  decideReturnRequest: decideReturnRequestMock,
  getReturnRequest: getReturnRequestMock,
  updateOrderStatus: updateOrderStatusMock,
}));
vi.mock("@/modules/orders/lifecycle", () => ({ isManagedOrderStatus: isManagedOrderStatusMock }));
vi.mock("@/modules/orders/return-lifecycle", () => ({
  availableReturnActions: availableReturnActionsMock,
}));

import { manageReturnRequestAction, updateOrderStatusAction } from "./actions";

const orderId = "11111111-1111-4111-8111-111111111111";
const returnRequestId = "22222222-2222-4222-8222-222222222222";

function returnRequest(overrides: Partial<ReturnRequest> = {}): ReturnRequest {
  return {
    id: returnRequestId,
    returnNumber: "RET-1001",
    orderId,
    type: 0,
    status: 0,
    refundTotal: 100,
    items: [],
    createdAt: "2026-08-22T14:30:00Z",
    ...overrides,
  };
}

function formData(intent: "approve" | "reject" | "receive" | "complete", decisionNote?: string): FormData {
  const data = new FormData();
  data.set("orderId", orderId);
  data.set("returnRequestId", returnRequestId);
  data.set("intent", intent);
  if (decisionNote !== undefined) data.set("decisionNote", decisionNote);
  return data;
}

describe("manageReturnRequestAction", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireAdminActionSessionMock.mockResolvedValue({ accessToken: "test-admin-token" });
    availableReturnActionsMock.mockImplementation((request: ReturnRequest) => {
      if (request.status === 0) return ["receive"];
      if (request.status === 3 && request.receivedAt && !request.approvedAt) return ["reject", "approve"];
      return [];
    });
    advanceReturnRequestMock.mockResolvedValue(returnRequest({
      status: 3,
      receivedAt: "2026-08-23T10:00:00Z",
    }));
    decideReturnRequestMock.mockResolvedValue(returnRequest({
      status: 1,
      receivedAt: "2026-08-23T10:00:00Z",
      approvedAt: "2026-08-23T10:05:00Z",
    }));
  });

  it("receives a requested return before exposing a decision", async () => {
    getReturnRequestMock.mockResolvedValue(returnRequest());

    const result = await manageReturnRequestAction(null, formData("receive"));

    expect(advanceReturnRequestMock).toHaveBeenCalledWith(
      returnRequestId,
      "receive",
      expect.objectContaining({ accessToken: "test-admin-token" }),
    );
    expect(decideReturnRequestMock).not.toHaveBeenCalled();
    expect(result).toMatchObject({
      status: "success",
      message: expect.stringContaining("karar bekliyor"),
      returnRequest: {
        status: 3,
        receivedAt: "2026-08-23T10:00:00Z",
      },
    });
  });

  it("approves only a received return awaiting decision and preserves the decision note", async () => {
    getReturnRequestMock.mockResolvedValue(returnRequest({
      status: 3,
      receivedAt: "2026-08-23T10:00:00Z",
    }));

    const result = await manageReturnRequestAction(null, formData("approve", "Kontrol edildi"));

    expect(decideReturnRequestMock).toHaveBeenCalledWith(
      returnRequestId,
      "approve",
      "Kontrol edildi",
      expect.objectContaining({ accessToken: "test-admin-token" }),
    );
    expect(result).toMatchObject({ status: "success", message: expect.stringContaining("Ücret İade Edildi") });
  });

  it("blocks a direct requested-to-approved submission before mutation", async () => {
    getReturnRequestMock.mockResolvedValue(returnRequest());

    const result = await manageReturnRequestAction(null, formData("approve"));

    expect(result).toMatchObject({ status: "error", refresh: true });
    expect(decideReturnRequestMock).not.toHaveBeenCalled();
    expect(advanceReturnRequestMock).not.toHaveBeenCalled();
  });

  it("maps a typed transition conflict separately and requests authoritative refresh", async () => {
    getReturnRequestMock.mockResolvedValue(returnRequest());
    advanceReturnRequestMock.mockRejectedValue(new ApiErrorMock({
      title: "Conflict",
      status: 409,
      code: "return_status_transition_invalid",
      traceId: "trace-return",
    }));

    const result = await manageReturnRequestAction(null, formData("receive"));

    expect(result).toEqual({
      status: "error",
      message: "İade talebinin durumu değişti. Güncel talebi inceleyip işlemi yeniden seçin.",
      traceId: "trace-return",
      refresh: true,
    });
  });
});

describe("updateOrderStatusAction", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireAdminActionSessionMock.mockResolvedValue({ accessToken: "test-admin-token" });
    isManagedOrderStatusMock.mockReturnValue(true);
  });

  it("returns the authoritative status from the PATCH response for immediate UI reconciliation", async () => {
    updateOrderStatusMock.mockResolvedValue({ status: 3 });
    const data = new FormData();
    data.set("orderId", orderId);
    data.set("status", "3");

    const result = await updateOrderStatusAction(null, data);

    expect(updateOrderStatusMock).toHaveBeenCalledWith(
      orderId,
      {
        status: 3,
        shippingCarrier: null,
        trackingNumber: null,
        trackingUrl: null,
      },
      expect.objectContaining({ accessToken: "test-admin-token" }),
    );
    expect(result).toEqual({
      status: "success",
      message: "Sipariş durumu güncellendi.",
      orderStatus: 3,
    });
  });
});
