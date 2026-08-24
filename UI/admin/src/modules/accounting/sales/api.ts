import "server-only";

import type { components } from "@/generated/api";
import { apiRequest } from "@/lib/api/client";
import type { PagedResult } from "@/lib/api/pagination";
import type { AdminSession } from "@/lib/auth/contracts";
import type {
  AccountingSalesOrder,
  AccountingSalesOrderHeaderInput,
  AccountingSalesOrderLineInput,
  AccountingSalesOrderPage,
  CurrentAccountOption,
  SalesInvoice,
  SalesInvoiceHeaderInput,
  SalesInvoicePage,
  SalesListQuery,
  SalesVariantOption,
} from "./types";

type Product = components["schemas"]["ProductDto"];

export function getSalesOrders(query: SalesListQuery, session: AdminSession): Promise<AccountingSalesOrderPage> {
  return apiRequest(`/api/accounting/sales-orders?PageNumber=${query.pageNumber}&PageSize=${query.pageSize}`, { accessToken: session.accessToken });
}

export function getSalesOrder(id: string, session: AdminSession): Promise<AccountingSalesOrder> {
  return apiRequest(`/api/accounting/sales-orders/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
}

export function createSalesOrder(header: AccountingSalesOrderHeaderInput, lines: AccountingSalesOrderLineInput[], createInvoice: boolean, invoice: SalesInvoiceHeaderInput | null, idempotencyKey: string, session: AdminSession): Promise<AccountingSalesOrder> {
  return apiRequest("/api/accounting/sales-orders", { method: "POST", body: { header, lines, createInvoice, invoice }, headers: { "Idempotency-Key": idempotencyKey }, accessToken: session.accessToken });
}

export function updateSalesOrder(id: string, header: AccountingSalesOrderHeaderInput, lines: AccountingSalesOrderLineInput[], session: AdminSession): Promise<AccountingSalesOrder> {
  return apiRequest(`/api/accounting/sales-orders/${encodeURIComponent(id)}`, { method: "PUT", body: { header, lines }, accessToken: session.accessToken });
}

export function postSalesOrder(id: string, session: AdminSession): Promise<AccountingSalesOrder> {
  return apiRequest(`/api/accounting/sales-orders/${encodeURIComponent(id)}/post`, { method: "POST", accessToken: session.accessToken });
}

export function cancelSalesOrder(id: string, reason: string, session: AdminSession): Promise<components["schemas"]["CancellationResultDto"]> {
  return apiRequest(`/api/accounting/sales-orders/${encodeURIComponent(id)}/cancel`, { method: "POST", body: { reason }, accessToken: session.accessToken });
}

export function getSalesInvoices(query: SalesListQuery, session: AdminSession): Promise<SalesInvoicePage> {
  return apiRequest(`/api/accounting/sales-invoices?PageNumber=${query.pageNumber}&PageSize=${query.pageSize}`, { accessToken: session.accessToken });
}

export function getSalesInvoice(id: string, session: AdminSession): Promise<SalesInvoice> {
  return apiRequest(`/api/accounting/sales-invoices/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
}

export function createDirectSalesInvoice(orderHeader: AccountingSalesOrderHeaderInput, invoiceHeader: SalesInvoiceHeaderInput, lines: AccountingSalesOrderLineInput[], idempotencyKey: string, session: AdminSession): Promise<SalesInvoice> {
  return apiRequest("/api/accounting/sales-invoices", { method: "POST", body: { orderHeader, invoiceHeader, lines }, headers: { "Idempotency-Key": idempotencyKey }, accessToken: session.accessToken });
}

export function createSalesInvoiceFromOrder(orderId: string, header: SalesInvoiceHeaderInput, session: AdminSession): Promise<SalesInvoice> {
  return apiRequest(`/api/accounting/sales-invoices/from-order/${encodeURIComponent(orderId)}`, { method: "POST", body: { header }, accessToken: session.accessToken });
}

export function updateSalesInvoice(id: string, header: SalesInvoiceHeaderInput, lines: AccountingSalesOrderLineInput[], session: AdminSession): Promise<SalesInvoice> {
  return apiRequest(`/api/accounting/sales-invoices/${encodeURIComponent(id)}`, { method: "PUT", body: { header, lines }, accessToken: session.accessToken });
}

export function postSalesInvoice(id: string, session: AdminSession): Promise<SalesInvoice> {
  return apiRequest(`/api/accounting/sales-invoices/${encodeURIComponent(id)}/post`, { method: "POST", accessToken: session.accessToken });
}

export async function getSalesLookups(session: AdminSession): Promise<{ customers: CurrentAccountOption[]; variants: SalesVariantOption[]; truncated: boolean }> {
  const [accounts, products] = await Promise.all([
    apiRequest<PagedResult<CurrentAccountOption>>("/api/accounting/current-accounts?PageNumber=1&PageSize=100", { accessToken: session.accessToken }),
    apiRequest<PagedResult<Product>>("/api/products?PageNumber=1&PageSize=100&SortBy=1&Descending=false", { accessToken: session.accessToken }),
  ]);
  return {
    customers: accounts.items.filter((account) => account.isActive && (account.type === 1 || account.type === 3)),
    variants: products.items.flatMap((product) => product.variants.filter((variant) => product.isActive && variant.isActive).map((variant) => ({ id: variant.id, productId: product.id, productName: product.title, variantName: `${variant.name}: ${variant.value}`, sku: variant.sku }))),
    truncated: accounts.totalCount > 100 || products.totalCount > 100,
  };
}
