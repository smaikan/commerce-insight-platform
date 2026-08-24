import type { components } from "@/generated/api";

export type CashAccount = components["schemas"]["CashAccountDto"];
export type BankAccount = components["schemas"]["BankAccountDto"];
export type FinancialTransaction = components["schemas"]["FinancialTransactionDto"];
export type FinancialAccountInput = components["schemas"]["FinancialAccountInput"];
export type BankAccountInput = components["schemas"]["BankAccountInput"];
export type FinancialTransactionInput = components["schemas"]["CreateFinancialTransactionInput"];
export type BankTransferInput = components["schemas"]["BankTransferInput"];
export type BankTransfer = components["schemas"]["BankTransferDto"];
export type CancellationResult = components["schemas"]["CancellationResultDto"];
export type TreasuryView = "accounts" | "manual" | "transfer";
export type TreasuryFormState = { status: "idle" | "success" | "error"; message?: string; fieldErrors?: Record<string, string[]>; traceId?: string; retryAfter?: string; refresh?: boolean; redirectHref?: string };
export const initialTreasuryFormState: TreasuryFormState = { status: "idle" };
