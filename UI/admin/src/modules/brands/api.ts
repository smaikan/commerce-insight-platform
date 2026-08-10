import "server-only";

import type { components } from "@/generated/api";
import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { Brand, BrandPage, CreateBrandInput, UpdateBrandInput } from "@/modules/brands/types";

// Burada marka listesini belgelenen sayfalama parametreleriyle yetkili ve taze veriden alıyorum.
export function getBrands(pageNumber: number, pageSize: number, session: AdminSession): Promise<BrandPage> {
  const params = new URLSearchParams({ PageNumber: String(pageNumber), PageSize: String(pageSize) });
  return apiRequest(`/api/brands?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada düzenleme ekranının marka detayını belgelenen kimlik endpoint'inden alıyorum.
export function getBrand(id: string, session: AdminSession): Promise<Brand> {
  return apiRequest(`/api/brands/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
}

// Burada yeni markayı yalnız yetkili sunucu sınırından oluşturuyorum.
export function createBrand(input: CreateBrandInput, session: AdminSession): Promise<Brand> {
  return apiRequest("/api/brands", { method: "POST", body: input, accessToken: session.accessToken });
}

// Burada marka bilgi ve görsel alanlarını aktiflikten bağımsız güncelliyorum.
export function updateBrand(id: string, input: UpdateBrandInput, session: AdminSession): Promise<Brand> {
  return apiRequest(`/api/brands/${encodeURIComponent(id)}`, { method: "PUT", body: input, accessToken: session.accessToken });
}

// Burada hiçbir üründe kullanılmayan markayı güvenli silme endpoint'ine gönderiyorum.
export function deleteBrand(id: string, session: AdminSession): Promise<void> {
  return apiRequest(`/api/brands/${encodeURIComponent(id)}`, { method: "DELETE", accessToken: session.accessToken });
}

// Burada marka aktifliğini belgelenen özel endpoint üzerinden değiştiriyorum.
export function setBrandActivation(id: string, isActive: boolean, session: AdminSession): Promise<Brand> {
  const body: components["schemas"]["SetActivationRequest"] = { isActive };
  return apiRequest(`/api/brands/${encodeURIComponent(id)}/activation`, {
    method: "PATCH",
    body,
    accessToken: session.accessToken,
  });
}
