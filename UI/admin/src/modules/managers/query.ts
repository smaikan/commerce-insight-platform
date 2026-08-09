import type { ManagerQuery } from "@/modules/managers/types";

// Burada yöneticiler listesi için URL sayfalama ve arama durumunu çözüyorum.
export function parseManagerQuery(params: Record<string, string | string[] | undefined>): ManagerQuery {
  const one = (value: string | string[] | undefined) => Array.isArray(value) ? value[0] : value;
  const page = Number(one(params.pageNumber)); const size = Number(one(params.pageSize));
  return { pageNumber: Number.isInteger(page) && page > 0 ? page : 1, pageSize: [20, 50, 100].includes(size) ? size : 20, search: one(params.search)?.trim() || undefined };
}

// Burada sayfalama bağlantısında yönetici aramasını koruyorum.
export function managerHref(query: ManagerQuery, pageNumber = query.pageNumber): string {
  const params = new URLSearchParams(); if (pageNumber > 1) params.set("pageNumber", String(pageNumber)); if (query.pageSize !== 20) params.set("pageSize", String(query.pageSize)); if (query.search) params.set("search", query.search); return params.size ? `/managers?${params}` : "/managers";
}
