import "server-only";

import { apiGet } from "@/lib/api/client";
import type { PublishedProduct, PublishedProductPage, PublishedProductQuery } from "@/modules/catalog/types";
import type {
  SearchInspiration,
  SearchProduct,
  SearchSuggestionQuery,
  SearchSuggestions,
} from "@/modules/search/types";

const SEARCH_SUGGESTION_LIMIT = 10;
const INSPIRATION_LIMIT = 5;

// Burada navbar sorgusunu OpenAPI parametreleriyle tek, cache dışı ve iptal edilebilir suggestion isteğine dönüştürüyorum.
export async function getSearchSuggestions(query: string, signal?: AbortSignal): Promise<SearchSuggestions> {
  const parameters: SearchSuggestionQuery = { Query: query, Limit: SEARCH_SUGGESTION_LIMIT };
  const search = new URLSearchParams({
    Query: parameters.Query,
    Limit: String(parameters.Limit),
  });

  return apiGet<SearchSuggestions>(`/api/products/published/search-suggestions?${search}`, {
    cache: "no-store",
    signal,
  });
}

// Burada ilham alanını backend'in belgeli Popularity sırasından tek bir küçük, paylaşımlı katalog isteğiyle alıyorum.
export async function getSearchInspiration(signal?: AbortSignal): Promise<SearchInspiration> {
  const parameters: PublishedProductQuery = {
    PageNumber: 1,
    PageSize: INSPIRATION_LIMIT,
    SortBy: 1,
    Descending: true,
  };
  const search = new URLSearchParams(
    Object.entries(parameters).map(([key, value]) => [key, String(value)]),
  );
  const page = await apiGet<PublishedProductPage>(`/api/products/published?${search}`, {
    revalidate: 60,
    tags: ["products", "published-products", "search-inspiration"],
    signal,
  });

  return { items: page.items.slice(0, INSPIRATION_LIMIT).map(toSearchProduct) };
}

// Burada katalog kartını yalnız modalın kullandığı OpenAPI suggestion projeksiyonuna kayıpsız biçimde eşliyorum.
function toSearchProduct(product: PublishedProduct): SearchProduct {
  return {
    id: product.id,
    title: product.title,
    url: product.url,
    brandName: product.brandName ?? null,
    price: product.price ?? null,
    compareAtPrice: product.compareAtPrice ?? null,
    imageUrl: product.mainImage?.imageUrl ?? null,
    imageAlt: product.mainImage?.altText ?? null,
    isAvailable: product.isAvailable,
  };
}
