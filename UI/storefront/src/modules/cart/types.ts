import type { components } from "@/generated/api";

export type Cart = components["schemas"]["CartDto"];
export type AddCartItemRequest = components["schemas"]["AddCartItemRequest"];
export type UpdateCartItemRequest = components["schemas"]["UpdateCartItemQuantityRequest"];

export type CartConcurrencyRequest = {
  expectedConcurrencyToken: string;
};

export type ClientProblem = {
  status: number;
  title: string;
  detail?: string;
  code?: string;
  traceId?: string;
};
