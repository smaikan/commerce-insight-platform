import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import { catalogResourceConfigs, type CatalogResource } from "@/modules/settings/catalog-resource";
import type { CatalogFormValue, CatalogItem, CatalogPage } from "@/modules/settings/catalog-types";
import type { SettingsListQuery } from "@/modules/settings/types";

// Burada seçilen katalog kaynağını aynı belgeli sayfalama sözleşmesiyle okuyorum.
export function getCatalogItems(resource: CatalogResource, query: SettingsListQuery, session: AdminSession): Promise<CatalogPage> {
  const config = catalogResourceConfigs[resource];
  const params = new URLSearchParams({ PageNumber: String(query.pageNumber), PageSize: String(query.pageSize) });
  return apiRequest(`${config.endpoint}?${params}`, { accessToken: session.accessToken });
}

// Burada seçilen katalog kaydını düzenleme formu için kimliğiyle okuyorum.
export function getCatalogItem(resource: CatalogResource, id: string, session: AdminSession): Promise<CatalogItem> {
  return apiRequest(`${catalogResourceConfigs[resource].endpoint}/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
}

// Burada kaynak bazlı create gövdesini yalnızca o endpoint'in kabul ettiği alanlarla oluşturuyorum.
export function createCatalogItem(resource: CatalogResource, value: CatalogFormValue, session: AdminSession): Promise<CatalogItem> {
  return apiRequest(catalogResourceConfigs[resource].endpoint, { method: "POST", body: createPayload(resource, value), accessToken: session.accessToken });
}

// Burada aktiflikten ayrı tutulan katalog bilgi alanlarını güncelliyorum.
export function updateCatalogItem(resource: CatalogResource, id: string, value: CatalogFormValue, session: AdminSession): Promise<CatalogItem> {
  return apiRequest(`${catalogResourceConfigs[resource].endpoint}/${encodeURIComponent(id)}`, { method: "PUT", body: updatePayload(resource, value), accessToken: session.accessToken });
}

// Burada üründe kullanılmayan tür veya etiketi kaynak endpoint'inden siliyorum.
export function deleteCatalogItem(resource: CatalogResource, id: string, session: AdminSession): Promise<void> {
  return apiRequest(`${catalogResourceConfigs[resource].endpoint}/${encodeURIComponent(id)}`, { method: "DELETE", accessToken: session.accessToken });
}

// Burada katalog kaydının kullanılabilirliğini ortak activation endpoint yapısıyla değiştiriyorum.
export function setCatalogItemActivation(resource: CatalogResource, id: string, isActive: boolean, session: AdminSession): Promise<CatalogItem> {
  return apiRequest(`${catalogResourceConfigs[resource].endpoint}/${encodeURIComponent(id)}/activation`, { method: "PATCH", body: { isActive }, accessToken: session.accessToken });
}

// Burada create isteğinde isActive alanını koruyup desteklenmeyen alanları gövdeden çıkarıyorum.
function createPayload(resource: CatalogResource, value: CatalogFormValue) {
  if (resource === "product-types") return { name: value.name, description: value.description, isActive: value.isActive };
  return { name: value.name, url: value.url, isActive: value.isActive };
}

// Burada update endpoint'lerinin kabul etmediği aktiflik alanını bilgi güncellemesinden ayırıyorum.
function updatePayload(resource: CatalogResource, value: CatalogFormValue) {
  if (resource === "product-types") return { name: value.name, description: value.description };
  return { name: value.name, url: value.url };
}
