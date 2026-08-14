import { beforeEach, describe, expect, it, vi } from "vitest";

const { apiRequestMock } = vi.hoisted(() => ({ apiRequestMock: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiRequest: apiRequestMock }));

import {
  getAdminStoreSettings,
  updateStoreContact,
  updateStoreIdentity,
  updateStoreLegal,
  updateStoreSeo,
  updateStorefrontPreferences,
} from "../api";
import type { AdminSession } from "@/lib/auth/contracts";

const session = { accessToken: "admin-token" } as AdminSession;

describe("store settings API client", () => {
  beforeEach(() => apiRequestMock.mockReset().mockResolvedValue({ concurrencyToken: "next" }));

  it("reads the admin-only settings endpoint", async () => {
    await getAdminStoreSettings(session);
    expect(apiRequestMock).toHaveBeenCalledWith("/api/store-settings/admin", { accessToken: "admin-token" });
  });

  it.each([
    ["identity", updateStoreIdentity],
    ["contact", updateStoreContact],
    ["legal", updateStoreLegal],
    ["seo", updateStoreSeo],
    ["storefront", updateStorefrontPreferences],
  ] as const)("updates only the %s endpoint", async (section, update) => {
    const body = { expectedConcurrencyToken: "token" } as never;
    await update(body, session);
    expect(apiRequestMock).toHaveBeenCalledWith(`/api/store-settings/${section}`, {
      method: "PUT",
      body,
      accessToken: "admin-token",
    });
  });
});
