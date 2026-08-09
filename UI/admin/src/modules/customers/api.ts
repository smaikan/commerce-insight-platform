import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { AdminUser, CustomerDetail, CustomerListQuery, CustomerOrderPage, UserPage, UserRole, UserStatus } from "@/modules/customers/types";

// Burada yönetici kullanıcı listesini yalnız belgelenmiş Search, Role, Status ve sayfalama parametreleriyle getiriyorum.
export function getCustomers(query: CustomerListQuery, session: AdminSession): Promise<UserPage> {
  const params = new URLSearchParams({
    PageNumber: String(query.pageNumber),
    PageSize: String(query.pageSize),
  });
  if (query.search) params.set("Search", query.search);
  if (query.role !== undefined) params.set("Role", String(query.role));
  if (query.status !== undefined) params.set("Status", String(query.status));
  return apiRequest(`/api/users?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada tek kullanıcı detayını public U-prefixli kimlikle yönetici yetkisiyle getiriyorum.
export function getCustomer(publicUserId: string, session: AdminSession): Promise<CustomerDetail> {
  return apiRequest(`/api/users/${encodeURIComponent(publicUserId)}`, {
    accessToken: session.accessToken,
  });
}

// Burada müşteri detayına ait sipariş özetlerini yeni admin endpointinden sayfalı olarak getiriyorum.
export function getCustomerOrders(publicUserId: string, session: AdminSession): Promise<CustomerOrderPage> {
  return apiRequest(`/api/users/${encodeURIComponent(publicUserId)}/orders?PageNumber=1&PageSize=10`, { accessToken: session.accessToken });
}

// Burada rol değişikliğini yalnızca yetkili Server Action sınırından API'ye iletiyorum.
export function updateCustomerRole(publicUserId: string, role: UserRole, session: AdminSession): Promise<AdminUser> {
  return apiRequest(`/api/users/${encodeURIComponent(publicUserId)}/role`, {
    method: "PATCH",
    body: { role },
    accessToken: session.accessToken,
  });
}

// Burada hesap durumunu belgelenen ayrı endpoint üzerinden güncelliyorum.
export function updateCustomerStatus(publicUserId: string, status: UserStatus, session: AdminSession): Promise<AdminUser> {
  return apiRequest(`/api/users/${encodeURIComponent(publicUserId)}/status`, {
    method: "PATCH",
    body: { status },
    accessToken: session.accessToken,
  });
}
