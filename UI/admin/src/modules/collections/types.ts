import type { components } from "@/generated/api";
import type { PagedResult } from "@/lib/api/pagination";

// Burada koleksiyon wire modelini üretilen sözleşmeden özellik modeline bağlıyorum.
export type Collection = components["schemas"]["CollectionDto"];

// Burada ortak sayfalama sözleşmesini yeniden yazmadan koleksiyon listesine uyarlıyorum.
export type CollectionPage = PagedResult<Collection>;

// Burada liste URL'sinin yalnız belgelenen sayfalama değerlerini taşımasını sağlıyorum.
export type CollectionListQuery = {
  pageNumber: number;
  pageSize: number;
};

// Burada koleksiyon formunun tam, kısmi ve hatalı kayıt sonuçlarını istemcide güvenle taşıyorum.
export type CollectionActionState = {
  status: "idle" | "success" | "partial" | "error";
  message?: string;
  fieldErrors?: Record<string, string[]>;
  traceId?: string;
  collectionId?: string;
};

export const initialCollectionActionState: CollectionActionState = { status: "idle" };
