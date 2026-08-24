import { describe, expect, it } from "vitest";
import { getAccountingReport } from "./catalog";
import { buildReportHref, parseReportQuery } from "./query";

// Burada rapor sorgularının allowlist dışındaki filtreleri ve ters tarih aralığını API'den uzak tuttuğunu doğruluyorum.
describe("accounting report query", () => {
  it("keeps only sales report filters", () => {
    const report = getAccountingReport("sales")!;
    const query = parseReportQuery({ pageNumber: "3", search: "  SAT-1 ", from: "2026-08-20", to: "2026-08-01", id: "invalid", hasSalesInvoice: "yes", scopeId: "77777777-7777-4777-8777-777777777777" }, report);
    expect(query).toMatchObject({ pageNumber: 3, search: "SAT-1", from: "2026-08-20", to: "", id: "", hasSalesInvoice: "yes", scopeId: "" });
    expect(buildReportHref(report, query)).toBe("/accounting/reports/sales?search=SAT-1&from=2026-08-20&hasSalesInvoice=yes&pageNumber=3");
  });

  it("ignores meaningless filters on VAT reports", () => {
    const query = parseReportQuery({ search: "x", id: "77777777-7777-4777-8777-777777777777", hasSalesInvoice: "yes" }, getAccountingReport("sales-vat")!);
    expect(query).toMatchObject({ search: "", id: "", hasSalesInvoice: "all" });
  });
});
