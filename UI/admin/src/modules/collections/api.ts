import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { Collection, CollectionPage } from "@/modules/collections/types";
import type { components } from "@/generated/api";

// Burada koleksiyon listesini belgelenen sayfalama parametreleriyle yetkili ve taze veriden alıyorum.
export function getCollections(pageNumber: number, pageSize: number, session: AdminSession): Promise<CollectionPage> {
  const params = new URLSearchParams({ PageNumber: String(pageNumber), PageSize: String(pageSize) });
  return apiRequest(`/api/collections?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada düzenleme ekranının koleksiyon detayını doğrudan belgelenen kimlik endpoint'inden alıyorum.
export function getCollection(id: string, session: AdminSession): Promise<Collection> {
  return apiRequest(`/api/collections/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
}

// Burada manuel koleksiyonu yalnızca yetkili sunucu sınırından oluşturuyorum.
export function createCollection(input: components["schemas"]["CreateCollectionCommand"], session: AdminSession): Promise<Collection> {
  return apiRequest("/api/collections", { method: "POST", body: input, accessToken: session.accessToken });
}

// Burada koleksiyonun içerik alanlarını aktiflik ve vitrin durumundan bağımsız güncelliyorum.
export function updateCollection(id: string, input: components["schemas"]["CollectionRequest"], session: AdminSession): Promise<Collection> {
  return apiRequest(`/api/collections/${encodeURIComponent(id)}`, { method: "PUT", body: input, accessToken: session.accessToken });
}

// Burada hiçbir ürüne bağlı olmayan koleksiyonu güvenli silme endpoint'ine gönderiyorum.
export function deleteCollection(id: string, session: AdminSession): Promise<void> {
  return apiRequest(`/api/collections/${encodeURIComponent(id)}`, { method: "DELETE", accessToken: session.accessToken });
}

// Burada koleksiyonun satışta kullanılabilirlik durumunu özel endpoint üzerinden değiştiriyorum.
export function setCollectionActivation(id: string, isActive: boolean, session: AdminSession): Promise<Collection> {
  return apiRequest(`/api/collections/${encodeURIComponent(id)}/activation`, { method: "PATCH", body: { isActive }, accessToken: session.accessToken });
}

// Burada koleksiyonun vitrinde öne çıkarılma durumunu özel endpoint üzerinden değiştiriyorum.
export function setCollectionFeatured(id: string, isFeatured: boolean, session: AdminSession): Promise<Collection> {
  return apiRequest(`/api/collections/${encodeURIComponent(id)}/featured`, { method: "PATCH", body: { isFeatured }, accessToken: session.accessToken });
}
