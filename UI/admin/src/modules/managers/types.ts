import type { components } from "@/generated/api";

export type Manager = components["schemas"]["AdminUserDto"];
// Burada GET /api/users yanıt şeması OpenAPI'de eksik olduğu için belgeli ortak sayfalama biçimini kullanıyorum.
export type ManagerPage = { items: Manager[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number; hasPreviousPage: boolean; hasNextPage: boolean };
export type RegisterManagerRequest = components["schemas"]["RegisterUserCommand"];

export type ManagerQuery = { pageNumber: number; pageSize: number; search?: string };
export type ManagerActionState = { status: "idle" | "error" | "partial"; message?: string; traceId?: string; fieldErrors?: Record<string, string[]> };
export const initialManagerActionState: ManagerActionState = { status: "idle" };
