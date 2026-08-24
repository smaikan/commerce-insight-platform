import type { components } from "@/generated/api";
import type { PagedResult } from "@/lib/api/pagination";
import type { AccountingReportRow } from "@/modules/accounting/core/types";

export type Payment = components["schemas"]["ECommerce.Application.Accounting.Payments.PaymentDto"];
export type PaymentSummary = components["schemas"]["PaymentSummaryDto"];
export type PaymentPage = PagedResult<PaymentSummary>;
export type PaymentInput = components["schemas"]["CreatePaymentInput"];
export type CurrentAccountOption = components["schemas"]["CurrentAccountDto"];
export type CashAccount = components["schemas"]["CashAccountDto"];
export type BankAccount = components["schemas"]["BankAccountDto"];
export type OpenItem = AccountingReportRow;

export type PaymentListQuery = { pageNumber: number; pageSize: number };
export type PaymentSetup = { type: 1 | 2; currentAccountId: string };
export type PaymentDraft = {
  idempotencyKey: string;
  currentAccountId: string;
  type: string;
  amount: string;
  paymentDate: string;
  accountKind: "cash" | "bank";
  financialAccountId: string;
  referenceNumber: string;
  description: string;
  allocations: Record<string, string>;
};
export type PaymentFormState = {
  status: "idle" | "success" | "error";
  message?: string;
  code?: string;
  traceId?: string;
  retryAfter?: string;
  fieldErrors?: Record<string, string[]>;
  draft?: PaymentDraft;
  redirectHref?: string;
  refresh?: boolean;
};
export const initialPaymentFormState: PaymentFormState = { status: "idle" };
