import "server-only";

import type { components } from "@/generated/api";
import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { AccountingReportPage } from "@/modules/accounting/core/types";
import type { CurrentAccount, CurrentAccountInput, CurrentAccountListQuery, CurrentAccountPage, CurrentAccountStatementQuery } from "@/modules/accounting/current-accounts/types";

export function getCurrentAccounts(query: CurrentAccountListQuery, session: AdminSession): Promise<CurrentAccountPage> {
  const params = new URLSearchParams({ PageNumber: String(query.pageNumber), PageSize: String(query.pageSize) });
  return apiRequest(`/api/accounting/current-accounts?${params}`, { accessToken: session.accessToken });
}

export function getCurrentAccount(id: string, session: AdminSession): Promise<CurrentAccount> {
  return apiRequest(`/api/accounting/current-accounts/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
}

export function createCurrentAccount(input: CurrentAccountInput, session: AdminSession): Promise<CurrentAccount> {
  return apiRequest("/api/accounting/current-accounts", { method: "POST", body: input, accessToken: session.accessToken });
}

export function updateCurrentAccount(id: string, account: CurrentAccountInput, isActive: boolean, session: AdminSession): Promise<CurrentAccount> {
  const body: components["schemas"]["UpdateCurrentAccountRequest"] = { account, isActive };
  return apiRequest(`/api/accounting/current-accounts/${encodeURIComponent(id)}`, { method: "PUT", body, accessToken: session.accessToken });
}

export function getCurrentAccountStatement(id: string, query: CurrentAccountStatementQuery, session: AdminSession): Promise<AccountingReportPage> {
  const params = new URLSearchParams({ PageNumber: String(query.statementPageNumber), PageSize: String(query.statementPageSize) });
  return apiRequest(`/api/accounting/reports/current-accounts/${encodeURIComponent(id)}/statement?${params}`, { accessToken: session.accessToken });
}
