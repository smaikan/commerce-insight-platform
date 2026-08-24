import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { AccountingReportPage } from "@/modules/accounting/core/types";
import type { BankAccount, CashAccount, CurrentAccountOption, Payment, PaymentInput, PaymentListQuery, PaymentPage, PaymentSetup } from "./types";
import type { PagedResult } from "@/lib/api/pagination";

export function getPayments(query: PaymentListQuery, session: AdminSession): Promise<PaymentPage> { return apiRequest(`/api/accounting/payments?PageNumber=${query.pageNumber}&PageSize=${query.pageSize}`, { accessToken: session.accessToken }); }
export function getPayment(id: string, session: AdminSession): Promise<Payment> { return apiRequest(`/api/accounting/payments/${encodeURIComponent(id)}`, { accessToken: session.accessToken }); }
export function createPayment(input: PaymentInput, idempotencyKey: string, session: AdminSession): Promise<Payment> { return apiRequest("/api/accounting/payments", { method: "POST", body: input, headers: { "Idempotency-Key": idempotencyKey }, accessToken: session.accessToken }); }
export function cancelPayment(id: string, reason: string, session: AdminSession): Promise<void> { return apiRequest(`/api/accounting/payments/${encodeURIComponent(id)}/cancel`, { method: "POST", body: { reason }, accessToken: session.accessToken }); }

export async function getPaymentLookups(session: AdminSession): Promise<{ accounts: CurrentAccountOption[]; cashAccounts: CashAccount[]; bankAccounts: BankAccount[]; truncated: boolean }> {
  const [accountPage, cashAccounts, bankAccounts] = await Promise.all([
    apiRequest<PagedResult<CurrentAccountOption>>("/api/accounting/current-accounts?PageNumber=1&PageSize=100", { accessToken: session.accessToken }),
    apiRequest<CashAccount[]>("/api/accounting/cash-accounts", { accessToken: session.accessToken }),
    apiRequest<BankAccount[]>("/api/accounting/bank-accounts", { accessToken: session.accessToken }),
  ]);
  return { accounts: accountPage.items.filter((item) => item.isActive), cashAccounts: cashAccounts.filter((item) => item.isActive), bankAccounts: bankAccounts.filter((item) => item.isActive), truncated: accountPage.totalCount > 100 };
}

export function getPaymentOpenItems(setup: PaymentSetup, session: AdminSession): Promise<AccountingReportPage> {
  const kind = setup.type === 1 ? "receivables" : "debts";
  return apiRequest(`/api/accounting/reports/${kind}?PageNumber=1&PageSize=100&Id=${encodeURIComponent(setup.currentAccountId)}`, { accessToken: session.accessToken });
}
