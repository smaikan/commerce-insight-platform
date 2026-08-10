import type { components } from "@/generated/api";
import type { PagedResult } from "@/lib/api/pagination";

// Burada marka wire modelini doğrudan üretilmiş OpenAPI sözleşmesine bağlıyorum.
export type Brand = components["schemas"]["BrandDto"];

// Burada ortak sayfalama sözleşmesini marka listesi için yeniden kullanıyorum.
export type BrandPage = PagedResult<Brand>;

// Burada marka listesinin yalnız belgelenmiş sayfalama durumunu URL'de tutuyorum.
export type BrandListQuery = {
  pageNumber: number;
  pageSize: number;
};

// Burada create ve update gövdelerini güncel OpenAPI tiplerinden ayırmadan kullanıyorum.
export type CreateBrandInput = components["schemas"]["CreateBrandCommand"];
export type UpdateBrandInput = components["schemas"]["BrandRequest"];

// Burada formun doğrulama, kısmi başarı ve yönlendirme durumlarını tek seri hale getirilebilir modelde tutuyorum.
export type BrandActionState = {
  status: "idle" | "created" | "success" | "partial" | "error";
  message?: string;
  traceId?: string;
  fieldErrors?: Record<string, string[]>;
  brandId?: string;
  redirectHref?: string;
};

export const initialBrandActionState: BrandActionState = { status: "idle" };
