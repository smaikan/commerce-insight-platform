import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type {
  AccountUser,
  AdminStoreSettings,
  ChangeEmailRequest,
  ChangePasswordRequest,
  CreateShippingMethodRequest,
  CreateTaxRateRequest,
  SettingsListQuery,
  ShippingMethod,
  ShippingMethodPage,
  TaxRate,
  TaxRatePage,
  UpdateProfileRequest,
  UpdateStoreContactRequest,
  UpdateStoreIdentityRequest,
  UpdateStoreLegalRequest,
  UpdateStoreSeoRequest,
  UpdateStorefrontPreferencesRequest,
  UpdateShippingMethodRequest,
  UpdateTaxRateRequest,
  UserSession,
} from "@/modules/settings/types";

export function getAdminStoreSettings(session: AdminSession): Promise<AdminStoreSettings> {
  return apiRequest("/api/store-settings/admin", { accessToken: session.accessToken });
}

export function updateStoreIdentity(payload: UpdateStoreIdentityRequest, session: AdminSession): Promise<AdminStoreSettings> {
  return apiRequest("/api/store-settings/identity", { method: "PUT", body: payload, accessToken: session.accessToken });
}

export function updateStoreContact(payload: UpdateStoreContactRequest, session: AdminSession): Promise<AdminStoreSettings> {
  return apiRequest("/api/store-settings/contact", { method: "PUT", body: payload, accessToken: session.accessToken });
}

export function updateStoreLegal(payload: UpdateStoreLegalRequest, session: AdminSession): Promise<AdminStoreSettings> {
  return apiRequest("/api/store-settings/legal", { method: "PUT", body: payload, accessToken: session.accessToken });
}

export function updateStoreSeo(payload: UpdateStoreSeoRequest, session: AdminSession): Promise<AdminStoreSettings> {
  return apiRequest("/api/store-settings/seo", { method: "PUT", body: payload, accessToken: session.accessToken });
}

export function updateStorefrontPreferences(payload: UpdateStorefrontPreferencesRequest, session: AdminSession): Promise<AdminStoreSettings> {
  return apiRequest("/api/store-settings/storefront", { method: "PUT", body: payload, accessToken: session.accessToken });
}

// Burada tüm kargo yöntemlerini belgeli sayfalama parametreleriyle okuyorum.
export function getShippingMethods(query: SettingsListQuery, session: AdminSession): Promise<ShippingMethodPage> {
  const params = new URLSearchParams({ pageNumber: String(query.pageNumber), pageSize: String(query.pageSize) });
  return apiRequest(`/api/shipping-methods?${params}`, { accessToken: session.accessToken });
}

// Burada tek kargo yöntemini düzenleme formu için kimliğiyle okuyorum.
export function getShippingMethod(id: string, session: AdminSession): Promise<ShippingMethod> {
  return apiRequest(`/api/shipping-methods/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
}

// Burada yeni kargo yöntemini yönetim sözleşmesiyle oluşturuyorum.
export function createShippingMethod(payload: CreateShippingMethodRequest, session: AdminSession): Promise<ShippingMethod> {
  return apiRequest("/api/shipping-methods", { method: "POST", body: payload, accessToken: session.accessToken });
}

// Burada kargo yönteminin düzenlenebilir alanlarını güncelliyorum.
export function updateShippingMethod(id: string, payload: UpdateShippingMethodRequest, session: AdminSession): Promise<ShippingMethod> {
  return apiRequest(`/api/shipping-methods/${encodeURIComponent(id)}`, { method: "PUT", body: payload, accessToken: session.accessToken });
}

// Burada kargo yönteminin checkout uygunluğunu dar activation endpoint'iyle değiştiriyorum.
export function setShippingMethodActivation(id: string, isActive: boolean, session: AdminSession): Promise<ShippingMethod> {
  return apiRequest(`/api/shipping-methods/${encodeURIComponent(id)}/activation`, { method: "PATCH", body: { isActive }, accessToken: session.accessToken });
}

// Burada tüm vergi oranlarını belgeli sayfalama parametreleriyle okuyorum.
export function getTaxRates(query: SettingsListQuery, session: AdminSession): Promise<TaxRatePage> {
  const params = new URLSearchParams({ pageNumber: String(query.pageNumber), pageSize: String(query.pageSize) });
  return apiRequest(`/api/tax-rates?${params}`, { accessToken: session.accessToken });
}

// Burada tek vergi oranını düzenleme formu için kimliğiyle okuyorum.
export function getTaxRate(id: string, session: AdminSession): Promise<TaxRate> {
  return apiRequest(`/api/tax-rates/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
}

// Burada yeni vergi oranını yönetim sözleşmesiyle oluşturuyorum.
export function createTaxRate(payload: CreateTaxRateRequest, session: AdminSession): Promise<TaxRate> {
  return apiRequest("/api/tax-rates", { method: "POST", body: payload, accessToken: session.accessToken });
}

// Burada vergi oranının düzenlenebilir alanlarını güncelliyorum.
export function updateTaxRate(id: string, payload: UpdateTaxRateRequest, session: AdminSession): Promise<TaxRate> {
  return apiRequest(`/api/tax-rates/${encodeURIComponent(id)}`, { method: "PUT", body: payload, accessToken: session.accessToken });
}

// Burada vergi oranının ürün seçimlerindeki uygunluğunu activation endpoint'iyle değiştiriyorum.
export function setTaxRateActivation(id: string, isActive: boolean, session: AdminSession): Promise<TaxRate> {
  return apiRequest(`/api/tax-rates/${encodeURIComponent(id)}/activation`, { method: "PATCH", body: { isActive }, accessToken: session.accessToken });
}

// Burada oturum açan yöneticinin güncel profilini okuyorum.
export function getAccount(session: AdminSession): Promise<AccountUser> {
  return apiRequest("/api/users/me", { accessToken: session.accessToken });
}

// Burada yöneticinin profil alanlarını kendi hesap endpoint'iyle güncelliyorum.
export function updateAccountProfile(payload: UpdateProfileRequest, session: AdminSession): Promise<AccountUser> {
  return apiRequest("/api/users/me/profile", { method: "PUT", body: payload, accessToken: session.accessToken });
}

// Burada yöneticinin e-posta değişikliğini mevcut parola doğrulamasıyla gönderiyorum.
export function changeAccountEmail(payload: ChangeEmailRequest, session: AdminSession): Promise<AccountUser> {
  return apiRequest("/api/users/me/email", { method: "PUT", body: payload, accessToken: session.accessToken });
}

// Burada yöneticinin parola değişikliğini yalnızca server-side API sınırından gönderiyorum.
export function changeAccountPassword(payload: ChangePasswordRequest, session: AdminSession): Promise<void> {
  return apiRequest("/api/users/me/password", { method: "PUT", body: payload, accessToken: session.accessToken });
}

// Burada oturum sahibinin aktif cihaz oturumlarını gizli token bilgisi taşımadan okuyorum.
export function getAccountSessions(session: AdminSession): Promise<UserSession[]> {
  return apiRequest("/api/users/me/sessions", { accessToken: session.accessToken });
}

// Burada yalnızca oturum sahibine ait seçili oturumu kimliğiyle sonlandırıyorum.
export function revokeAccountSession(sessionId: string, session: AdminSession): Promise<void> {
  return apiRequest(`/api/users/me/sessions/${encodeURIComponent(sessionId)}`, { method: "DELETE", accessToken: session.accessToken });
}

// Burada kullanıcının mevcut oturumu dahil bütün aktif oturumlarını backend'de geçersiz kılıyorum.
export function revokeAllAccountSessions(session: AdminSession): Promise<void> {
  return apiRequest("/api/users/me/sessions", { method: "DELETE", accessToken: session.accessToken });
}
