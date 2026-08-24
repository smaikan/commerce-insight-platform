import { beforeEach, describe, expect, it, vi } from "vitest";

const { createMock, updateMock, sessionMock, revalidateMock, parseFormMock } = vi.hoisted(() => ({ createMock: vi.fn(), updateMock: vi.fn(), sessionMock: vi.fn(), revalidateMock: vi.fn(), parseFormMock: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("next/cache", () => ({ revalidatePath: revalidateMock }));
vi.mock("@/lib/api/problem", () => ({ ApiError: class ApiError extends Error { problem = { status: 500, title: "Test error" }; } }));
vi.mock("@/lib/auth/session", () => ({ requireAdminActionSession: sessionMock }));
vi.mock("@/modules/accounting/current-accounts/api", () => ({ createCurrentAccount: createMock, updateCurrentAccount: updateMock }));
vi.mock("@/modules/accounting/current-accounts/form-data", () => ({ parseCurrentAccountForm: parseFormMock }));

import { saveCurrentAccountAction } from "./actions";

function form(active = false): FormData {
  const data = new FormData();
  data.set("code", "CR-001");
  data.set("name", "Örnek Cari");
  data.set("type", "1");
  if (active) data.set("isActive", "on");
  return data;
}

describe("current account save action", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionMock.mockResolvedValue({ accessToken: "admin-test-token" });
    parseFormMock.mockReturnValue({ ok: true, input: { code: "CR-001", name: "Örnek Cari", type: 1 } });
  });

  it("creates through the accounting API and returns an authoritative detail redirect", async () => {
    createMock.mockResolvedValue({ id: "account/id" });
    const result = await saveCurrentAccountAction(undefined, { status: "idle" }, form());
    expect(createMock).toHaveBeenCalledWith(expect.objectContaining({ code: "CR-001", type: 1 }), expect.objectContaining({ accessToken: "admin-test-token" }));
    expect(updateMock).not.toHaveBeenCalled();
    expect(result.redirectHref).toBe("/accounting/current-accounts/account%2Fid?created=1");
    expect(revalidateMock).toHaveBeenCalledWith("/accounting/current-accounts");
  });

  it("sends the explicit edit-only active state on update", async () => {
    updateMock.mockResolvedValue({ id: "account-id" });
    await saveCurrentAccountAction("account-id", { status: "idle" }, form(false));
    expect(updateMock).toHaveBeenCalledWith("account-id", expect.objectContaining({ code: "CR-001" }), false, expect.anything());
  });

  it("stops before auth and API calls when validation fails", async () => {
    parseFormMock.mockReturnValue({ ok: false, state: { status: "error", message: "İşaretli alanları kontrol edin." } });
    const result = await saveCurrentAccountAction(undefined, { status: "idle" }, new FormData());
    expect(result).toMatchObject({ status: "error", message: "İşaretli alanları kontrol edin." });
    expect(sessionMock).not.toHaveBeenCalled();
    expect(createMock).not.toHaveBeenCalled();
  });
});
