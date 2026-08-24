import "server-only";

import { authenticatedApiRequest } from "@/lib/api/authenticated-client";
import type {
  AccountAddress,
  AccountOrder,
  AccountOrderPage,
  AccountSession,
  AccountReturn,
  AccountReturnPage,
  AccountUser,
  AddressPayload,
  ProductVariantPage,
  ReturnRequestPayload,
} from "@/modules/account/contracts";

export type OrderListQuery = {
  pageNumber?: number;
  pageSize?: number;
  status?: number;
};

// Burada oturumdaki müşterinin profilini owner-scoped `/me` sözleşmesinden okuyorum.
export function getAccountUser(): Promise<AccountUser> {
  return authenticatedApiRequest<AccountUser>("/api/users/me");
}

// Burada müşterinin kendi profil alanlarını API otoritesinde güncelliyorum.
export function updateAccountUser(payload: { firstName: string; lastName: string; phoneNumber: string | null }): Promise<AccountUser> {
  return authenticatedApiRequest<AccountUser>("/api/users/me/profile", { method: "PUT", body: payload });
}

// Burada müşterinin yalnız kendisine ait adreslerini cache dışı listeliyorum.
export function getAccountAddresses(): Promise<AccountAddress[]> {
  return authenticatedApiRequest<AccountAddress[]>("/api/addresses");
}

// Burada yeni teslimat veya fatura adresini owner-scoped endpoint üzerinden oluşturuyorum.
export function createAccountAddress(payload: AddressPayload): Promise<AccountAddress> {
  return authenticatedApiRequest<AccountAddress>("/api/addresses", { method: "POST", body: payload });
}

// Burada seçili adresi kullanıcı sahipliği denetimini API'ye bırakarak güncelliyorum.
export function updateAccountAddress(id: string, payload: AddressPayload): Promise<AccountAddress> {
  return authenticatedApiRequest<AccountAddress>(`/api/addresses/${encodeURIComponent(id)}`, { method: "PUT", body: payload });
}

// Burada adresi kendi türü içinde varsayılan yapmak için dar kapsamlı endpointi çağırıyorum.
export function setDefaultAccountAddress(id: string): Promise<AccountAddress> {
  return authenticatedApiRequest<AccountAddress>(`/api/addresses/${encodeURIComponent(id)}/default`, { method: "PATCH" });
}

// Burada müşterinin sahip olduğu adresi API üzerinden kaldırıyorum.
export function deleteAccountAddress(id: string): Promise<void> {
  return authenticatedApiRequest<void>(`/api/addresses/${encodeURIComponent(id)}`, { method: "DELETE" });
}

// Burada sipariş filtrelerini yalnız belgelenmiş query alanlarıyla oluşturarak müşterinin özetlerini alıyorum.
export function getAccountOrders(query: OrderListQuery = {}): Promise<AccountOrderPage> {
  const params = new URLSearchParams();
  params.set("PageNumber", String(query.pageNumber ?? 1));
  params.set("PageSize", String(query.pageSize ?? 10));
  if (query.status !== undefined) params.set("Status", String(query.status));
  return authenticatedApiRequest<AccountOrderPage>(`/api/orders/mine?${params.toString()}`);
}

// Burada sipariş detayını backend'in kullanıcı sahipliği denetimli endpointinden getiriyorum.
export function getAccountOrder(id: string): Promise<AccountOrder> {
  return authenticatedApiRequest<AccountOrder>(`/api/orders/${encodeURIComponent(id)}`);
}

// Burada müşterinin kendi iade ve değişim taleplerini API sahiplik filtresiyle sayfalı okuyorum.
export function getAccountReturns(pageNumber = 1, pageSize = 10): Promise<AccountReturnPage> {
  return authenticatedApiRequest<AccountReturnPage>(`/api/returns/mine?PageNumber=${pageNumber}&PageSize=${pageSize}`);
}

// Burada tek iade talebini yalnız oturumdaki müşterinin erişebildiği endpointten getiriyorum.
export function getAccountReturn(id: string): Promise<AccountReturn> {
  return authenticatedApiRequest<AccountReturn>(`/api/returns/${encodeURIComponent(id)}`);
}

// Burada iade veya değişim talebini belgelenmiş kalem, adet ve replacement alanlarıyla oluşturuyorum.
export function createAccountReturn(payload: ReturnRequestPayload): Promise<AccountReturn> {
  return authenticatedApiRequest<AccountReturn>("/api/returns", { method: "POST", body: payload });
}

// Burada değişim formunun aynı ürüne ait canlı varyant seçeneklerini public ürün sözleşmesinden okuyorum.
export function getProductVariants(productId: string): Promise<ProductVariantPage> {
  return authenticatedApiRequest<ProductVariantPage>(`/api/product-variants/by-product/${encodeURIComponent(productId)}?pageNumber=1&pageSize=100`);
}

// Burada mevcut parolayı doğrulatarak yeni parolayı yalnız kullanıcıya ait güvenli endpointte değiştiriyorum.
export function changeAccountPassword(payload: { currentPassword: string; newPassword: string }): Promise<void> {
  return authenticatedApiRequest<void>("/api/users/me/password", { method: "PUT", body: payload });
}

// Burada token değerlerini açmadan kullanıcının aktif oturum özetlerini cache dışı okuyorum.
export function getAccountSessions(): Promise<AccountSession[]> {
  return authenticatedApiRequest<AccountSession[]>("/api/users/me/sessions");
}

// Burada yalnız kullanıcının sahip olduğu seçili oturumu API otoritesiyle kapatıyorum.
export function revokeAccountSession(id: string): Promise<void> {
  return authenticatedApiRequest<void>(`/api/users/me/sessions/${encodeURIComponent(id)}`, { method: "DELETE" });
}

// Burada API'nin tüm aktif refresh tokenları iptal eden oturum kapatma akışını çağırıyorum.
export function logoutAllAccountSessions(): Promise<void> {
  return authenticatedApiRequest<void>("/api/users/me/sessions", { method: "DELETE" });
}
