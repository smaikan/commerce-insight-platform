import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/problem";

const { requireAdminActionSessionMock, getAdminWorkQueueSummaryMock } = vi.hoisted(() => ({
  requireAdminActionSessionMock: vi.fn(),
  getAdminWorkQueueSummaryMock: vi.fn(),
}));

vi.mock("@/lib/auth/session", () => ({ requireAdminActionSession: requireAdminActionSessionMock }));
vi.mock("@/modules/dashboard/api", () => ({ getAdminWorkQueueSummary: getAdminWorkQueueSummaryMock }));

import { GET } from "./route";

describe("GET /api/admin/work-queue-summary", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireAdminActionSessionMock.mockResolvedValue({ accessToken: "server-only-token" });
  });

  // Burada doğrulanmış oturumla yalnız sayaç sözleşmesinin no-store olarak döndüğünü doğruluyorum.
  it("returns the work queue without exposing the access token", async () => {
    getAdminWorkQueueSummaryMock.mockResolvedValue({
      ordersAwaitingProcessingCount: 3,
      newContactMessageCount: 1,
      generatedAtUtc: "2026-08-27T10:00:00Z",
    });

    const response = await GET();
    const body = await response.json();

    expect(response.status).toBe(200);
    expect(response.headers.get("cache-control")).toBe("private, no-store, max-age=0");
    expect(body).toEqual({
      ordersAwaitingProcessingCount: 3,
      newContactMessageCount: 1,
      generatedAtUtc: "2026-08-27T10:00:00Z",
    });
    expect(JSON.stringify(body)).not.toContain("server-only-token");
  });

  // Burada oturum hatasının upstream ayrıntısı sızdırılmadan doğru HTTP durumuyla döndüğünü doğruluyorum.
  it("maps an expired session to a safe unauthorized response", async () => {
    requireAdminActionSessionMock.mockRejectedValue(new ApiError({
      title: "Unauthorized",
      status: 401,
      detail: "sensitive upstream detail",
    }));

    const response = await GET();
    const body = await response.json();

    expect(response.status).toBe(401);
    expect(body.message).toContain("Oturumunuz sona erdi");
    expect(JSON.stringify(body)).not.toContain("sensitive upstream detail");
  });
});
