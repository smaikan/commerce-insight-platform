import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AdminSession } from "@/lib/auth/contracts";
import type { CurrentAccountInput } from "./types";

const { apiRequestMock } = vi.hoisted(() => ({ apiRequestMock: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiRequest: apiRequestMock }));

import { createCurrentAccount, getCurrentAccount, getCurrentAccounts, getCurrentAccountStatement, updateCurrentAccount } from "./api";

const session: AdminSession = { accessToken: "admin-test-token", user: { id: "U00001", email: "admin@example.com", firstName: "Admin", lastName: "User", role: 2, status: 1, createdAt: "2026-08-24T00:00:00Z" } };
const input: CurrentAccountInput = { code: "CR-001", type: 1, name: "Örnek Cari", tradeName: null, nationalIdentityNumber: null, taxNumber: null, taxOffice: null, phoneNumber: null, email: null, country: null, city: null, district: null, neighborhood: null, addressLine: null, postalCode: null, userId: null };

describe("current account API adapter", () => {
  beforeEach(() => apiRequestMock.mockReset());

  it("sends only documented list and statement pagination parameters", async () => {
    apiRequestMock.mockResolvedValue({ items: [] });
    await getCurrentAccounts({ pageNumber: 2, pageSize: 50 }, session);
    await getCurrentAccountStatement("a/b", { statementPageNumber: 3, statementPageSize: 25 }, session);
    expect(apiRequestMock).toHaveBeenNthCalledWith(1, "/api/accounting/current-accounts?PageNumber=2&PageSize=50", { accessToken: session.accessToken });
    expect(apiRequestMock).toHaveBeenNthCalledWith(2, "/api/accounting/reports/current-accounts/a%2Fb/statement?PageNumber=3&PageSize=25", { accessToken: session.accessToken });
  });

  it("encodes identifiers and preserves create/update request shapes", async () => {
    apiRequestMock.mockResolvedValue({ id: "id" });
    await getCurrentAccount("a/b", session);
    await createCurrentAccount(input, session);
    await updateCurrentAccount("a/b", input, false, session);
    expect(apiRequestMock).toHaveBeenNthCalledWith(1, "/api/accounting/current-accounts/a%2Fb", { accessToken: session.accessToken });
    expect(apiRequestMock).toHaveBeenNthCalledWith(2, "/api/accounting/current-accounts", { method: "POST", body: input, accessToken: session.accessToken });
    expect(apiRequestMock).toHaveBeenNthCalledWith(3, "/api/accounting/current-accounts/a%2Fb", { method: "PUT", body: { account: input, isActive: false }, accessToken: session.accessToken });
  });
});
