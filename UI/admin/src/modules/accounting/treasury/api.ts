import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { BankAccount, BankAccountInput, BankTransfer, BankTransferInput, CancellationResult, CashAccount, FinancialAccountInput, FinancialTransaction, FinancialTransactionInput } from "./types";

export function getCashAccounts(session: AdminSession): Promise<CashAccount[]> { return apiRequest("/api/accounting/cash-accounts", { accessToken: session.accessToken }); }
export function createCashAccount(input: FinancialAccountInput, session: AdminSession): Promise<CashAccount> { return apiRequest("/api/accounting/cash-accounts", { method: "POST", body: input, accessToken: session.accessToken }); }
export function getCashStatement(id: string, session: AdminSession): Promise<FinancialTransaction[]> { return apiRequest(`/api/accounting/cash-accounts/${encodeURIComponent(id)}/statement`, { accessToken: session.accessToken }); }
export function getBankAccounts(session: AdminSession): Promise<BankAccount[]> { return apiRequest("/api/accounting/bank-accounts", { accessToken: session.accessToken }); }
export function createBankAccount(input: BankAccountInput, session: AdminSession): Promise<BankAccount> { return apiRequest("/api/accounting/bank-accounts", { method: "POST", body: input, accessToken: session.accessToken }); }
export function getBankStatement(id: string, session: AdminSession): Promise<FinancialTransaction[]> { return apiRequest(`/api/accounting/bank-accounts/${encodeURIComponent(id)}/statement`, { accessToken: session.accessToken }); }
export function createFinancialTransaction(input: FinancialTransactionInput, idempotencyKey: string, session: AdminSession): Promise<FinancialTransaction> { return apiRequest("/api/accounting/financial-transactions", { method: "POST", body: input, headers: { "Idempotency-Key": idempotencyKey }, accessToken: session.accessToken }); }
export function createBankTransfer(input: BankTransferInput, idempotencyKey: string, session: AdminSession): Promise<BankTransfer> { return apiRequest("/api/accounting/financial-transactions/bank-transfers", { method: "POST", body: input, headers: { "Idempotency-Key": idempotencyKey }, accessToken: session.accessToken }); }
export function reverseFinancialTransaction(id: string, reason: string, session: AdminSession): Promise<CancellationResult> { return apiRequest(`/api/accounting/financial-transactions/${encodeURIComponent(id)}/reverse`, { method: "POST", body: { reason }, accessToken: session.accessToken }); }
