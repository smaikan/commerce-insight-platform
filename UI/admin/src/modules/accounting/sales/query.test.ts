import { describe, expect, it } from "vitest";
import { buildSalesInvoiceListHref, buildSalesOrderListHref, canonicalSalesPage, parseSalesListQuery } from "./query";

describe("sales register URL state", () => {
  it("keeps only the server-supported page input", () => {
    expect(parseSalesListQuery({ pageNumber: "3", status: "2", search: "ignored" })).toEqual({ pageNumber: 3, pageSize: 20 });
    expect(buildSalesOrderListHref({ pageNumber: 2, pageSize: 20 })).toBe("/accounting/sales-orders?pageNumber=2");
    expect(buildSalesInvoiceListHref({ pageNumber: 1, pageSize: 20 })).toBe("/accounting/sales-invoices");
  });
  it("canonicalizes empty and out-of-range pages", () => {
    expect(canonicalSalesPage(4, 2)).toBe(2);
    expect(canonicalSalesPage(2, 0)).toBe(1);
    expect(canonicalSalesPage(2, 2)).toBeNull();
  });
});
