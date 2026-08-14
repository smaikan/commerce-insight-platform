import type { components, paths } from "@/generated/api";

// Burada arama wire tiplerini güncel OpenAPI üretiminden doğrudan alarak geçici DTO kopyalarını kaldırıyorum.
export type SearchProduct = components["schemas"]["PublishedProductSearchSuggestionItemDto"];
export type SearchSuggestions = components["schemas"]["PublishedProductSearchSuggestionsDto"];
export type SearchSuggestionQuery = NonNullable<
  paths["/api/products/published/search-suggestions"]["get"]["parameters"]["query"]
>;
export type SearchInspiration = { items: SearchProduct[] };

// Burada aynı-origin arama sınırından istemciye yalnız güvenli ProblemDetails alanlarını taşıyorum.
export type SearchClientProblem = {
  status: number;
  title: string;
  detail?: string;
  code?: string;
  traceId?: string;
  retryAfter?: string;
};
