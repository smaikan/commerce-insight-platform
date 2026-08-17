import type { components } from "@/generated/api";

export type AccountUser = components["schemas"]["UserDto"];
export type AccountAddress = components["schemas"]["AddressDto"];
export type AddressPayload = components["schemas"]["AddressRequest"];
export type AccountOrderSummary = components["schemas"]["OrderSummaryDto"];
export type AccountOrderPage = components["schemas"]["OrderSummaryDtoPagedResult"];
export type AccountOrder = components["schemas"]["OrderDto"];
export type AccountSession = components["schemas"]["UserSessionDto"];
export type AccountReturn = components["schemas"]["ReturnRequestDto"];
export type AccountReturnSummary = components["schemas"]["ReturnRequestSummaryDto"];
export type AccountReturnPage = components["schemas"]["ReturnRequestSummaryDtoPagedResult"];
export type ReturnRequestPayload = components["schemas"]["CreateReturnRequestRequest"];
export type ProductVariant = components["schemas"]["ProductVariantDto"];

export type ProductVariantPage = {
  items: ProductVariant[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export type AccountActionState = {
  status: "idle" | "success" | "error";
  revision: number;
  message?: string;
  fieldErrors?: Record<string, string>;
};

export const INITIAL_ACCOUNT_ACTION_STATE: AccountActionState = {
  status: "idle",
  revision: 0,
};
