import { describe, expect, it } from "vitest";
import { buildExpenseListHref, buildPurchaseInvoiceListHref, canonicalPageNumber, parseExpenseListQuery, parsePurchaseInvoiceListQuery } from "./query";

describe("purchase and expense URL state", () => {
  it("keeps only documented purchase invoice pagination", () => {
    expect(parsePurchaseInvoiceListQuery({ pageNumber: "3", search: "unsupported" })).toEqual({ pageNumber: 3, pageSize: 20 });
    expect(buildPurchaseInvoiceListHref({ pageNumber: 2, pageSize: 20 })).toBe("/accounting/purchase-invoices?pageNumber=2");
  });

  it("keeps expense and category registers independently shareable", () => {
    const query = parseExpenseListQuery({ view: "categories", expensePageNumber: "2", categoryPageNumber: "3" });
    expect(query).toEqual({ view: "categories", expensePageNumber: 2, categoryPageNumber: 3, pageSize: 20 });
    expect(buildExpenseListHref(query)).toBe("/accounting/expenses?view=categories&expensePageNumber=2&categoryPageNumber=3");
  });

  it("canonicalizes empty and out-of-range result pages", () => {
    expect(canonicalPageNumber(4, 2)).toBe(2);
    expect(canonicalPageNumber(2, 0)).toBe(1);
    expect(canonicalPageNumber(2, 2)).toBeNull();
  });
});
