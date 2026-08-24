import type { components } from "@/generated/api";
import type { PagedResult } from "@/lib/api/pagination";

export type CurrentAccount = components["schemas"]["CurrentAccountDto"];
export type CurrentAccountInput = components["schemas"]["CurrentAccountInput"];
export type CurrentAccountPage = PagedResult<CurrentAccount>;
export type CurrentAccountType = 1 | 2 | 3;
export type CurrentAccountListQuery = { pageNumber: number; pageSize: number };
export type CurrentAccountStatementQuery = { statementPageNumber: number; statementPageSize: number };
type CurrentAccountTextField = Exclude<keyof CurrentAccountInput, "type">;
export type CurrentAccountFormDraft = Record<CurrentAccountTextField, string> & { type: string; isActive: boolean };
export type CurrentAccountFormState = {
  status: "idle" | "success" | "error";
  message?: string;
  traceId?: string;
  retryAfter?: string;
  code?: string;
  fieldErrors?: Record<string, string[]>;
  draft?: CurrentAccountFormDraft;
  redirectHref?: string;
};

export const initialCurrentAccountFormState: CurrentAccountFormState = { status: "idle" };
