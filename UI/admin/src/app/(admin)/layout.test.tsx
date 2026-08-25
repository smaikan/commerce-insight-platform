import { beforeEach, describe, expect, it, vi } from "vitest";
import { ADMIN_RETURN_TO_HEADER } from "../../lib/auth/constants";

const { headersMock, requireAdminPageSessionMock, getAdminStoreSettingsMock } = vi.hoisted(() => ({
  headersMock: vi.fn(),
  requireAdminPageSessionMock: vi.fn(),
  getAdminStoreSettingsMock: vi.fn(),
}));

vi.mock("next/headers", () => ({ headers: headersMock }));
vi.mock("@/lib/auth/constants", () => import("../../lib/auth/constants"));
vi.mock("@/lib/auth/policy", () => import("../../lib/auth/policy"));
vi.mock("@/lib/auth/session", () => ({ requireAdminPageSession: requireAdminPageSessionMock }));
vi.mock("@/modules/settings/api", () => ({ getAdminStoreSettings: getAdminStoreSettingsMock }));
vi.mock("@/modules/admin-shell/components/admin-shell", () => ({ AdminShell: () => null }));

import AdminLayout from "./layout";

describe("AdminLayout auth return target", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireAdminPageSessionMock.mockResolvedValue({
      accessToken: "server-only-token",
      user: { firstName: "System", lastName: "Admin" },
    });
    getAdminStoreSettingsMock.mockResolvedValue({ displayName: "Eleven" });
  });

  // Burada layout 401 kontrolünün sabit dashboard yerine Proxy'den gelen tam Admin adresini kullandığını doğruluyorum.
  it("keeps route pagination and filters while verifying the session", async () => {
    headersMock.mockResolvedValue(new Headers({
      [ADMIN_RETURN_TO_HEADER]: "/accounting/payments?pageNumber=2&type=1",
    }));

    await AdminLayout({ children: null });

    expect(requireAdminPageSessionMock).toHaveBeenCalledWith("/accounting/payments?pageNumber=2&type=1");
  });

  // Burada Proxy headerı bulunmayan doğrudan renderlarda güvenli dashboard varsayımını koruyorum.
  it("falls back to dashboard without a trusted request target", async () => {
    headersMock.mockResolvedValue(new Headers());

    await AdminLayout({ children: null });

    expect(requireAdminPageSessionMock).toHaveBeenCalledWith("/dashboard");
  });
});
