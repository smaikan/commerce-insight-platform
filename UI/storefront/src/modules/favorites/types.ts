import type { components } from "@/generated/api";

export type FavoriteProduct = components["schemas"]["ProductDto"];
export type FavoriteProductPage = components["schemas"]["ProductDtoPagedResult"];

export type FavoriteState = {
  productIds: string[];
  totalCount: number;
};

export type FavoriteClientProblem = {
  status: number;
  title: string;
  detail?: string;
  code?: string;
  traceId?: string;
  retryAfter?: string;
};
