import "server-only";
import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type { Manager, ManagerPage, ManagerQuery, RegisterManagerRequest } from "@/modules/managers/types";

// Burada yalnız Admin rolündeki kullanıcıları belgeli rol filtresiyle listeliyorum.
export function getManagers(query: ManagerQuery, session: AdminSession): Promise<ManagerPage> {
  const params = new URLSearchParams({ PageNumber: String(query.pageNumber), PageSize: String(query.pageSize), Role: "2" });
  if (query.search) params.set("Search", query.search);
  return apiRequest(`/api/users?${params}`, { accessToken: session.accessToken });
}

// Burada kayıt endpointiyle kullanıcıyı oluşturuyorum; rol yükseltme ayrı Admin endpointinde yapılır.
export function registerManager(payload: RegisterManagerRequest, session: AdminSession) {
  return apiRequest<{ user: { id: string } }>("/api/auth/register", { method: "POST", body: payload, accessToken: session.accessToken });
}

// Burada yeni kullanıcıyı Admin rolüne yükseltiyorum.
export function promoteToAdmin(id: string, session: AdminSession): Promise<Manager> {
  return apiRequest<Manager>(`/api/users/${encodeURIComponent(id)}/role`, { method: "PATCH", body: { role: 2 }, accessToken: session.accessToken });
}
