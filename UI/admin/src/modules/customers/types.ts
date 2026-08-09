import type { components } from "@/generated/api";

// Burada müşteri wire modellerini generated OpenAPI şemalarından okunabilir feature adlarına bağlıyorum.
export type AdminUser = components["schemas"]["AdminUserDto"];
// Burada detay endpointinin döndürdüğü kullanıcı modelini liste modelinden ayrı tutuyorum.
export type CustomerDetail = components["schemas"]["UserDto"];
// Burada müşteri detayında kullanılan sipariş özet sayfasını generated sözleşmeye bağlıyorum.
export type CustomerOrderPage = components["schemas"]["OrderSummaryDtoPagedResult"];
export type UserRole = components["schemas"]["UserRole"];
export type UserStatus = components["schemas"]["UserStatus"];

// Burada OpenAPI'de GET /api/users response body eksik olduğu için ortak sayfalama yapısını belgelen şema alanlarıyla tanımlıyorum.
export type UserPage = {
  items: AdminUser[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

// Burada belgelenmiş Search, Role, Status ve sayfalama filtrelerini tek sorgu tipinde topluyorum.
export type CustomerListQuery = {
  pageNumber: number;
  pageSize: number;
  search?: string;
  role?: UserRole;
  status?: UserStatus;
};
