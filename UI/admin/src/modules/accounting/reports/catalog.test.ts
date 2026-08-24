import { describe, expect, it } from "vitest";
import { accountingReportCatalog } from "./catalog";

// Burada 28 yayımlanmış endpointin tekil slug ve rapora özgü kolon sözlükleriyle eksiksiz temsil edildiğini doğruluyorum.
describe("accounting report catalog", () => {
  it("contains all report endpoints once", () => {
    expect(accountingReportCatalog).toHaveLength(28);
    expect(new Set(accountingReportCatalog.map((report) => report.slug)).size).toBe(28);
    expect(new Set(accountingReportCatalog.map((report) => report.endpoint)).size).toBe(28);
    expect(accountingReportCatalog.every((report) => report.columns.length > 0)).toBe(true);
  });

  it("keeps VAT and profitability semantics separate", () => {
    expect(accountingReportCatalog.find((report) => report.slug === "sales-vat")?.columns.map((column) => column.label)).toEqual(["KDV oranı", "KDV hariç matrah", "KDV tutarı", "KDV dahil toplam", "Satır sayısı"]);
    expect(accountingReportCatalog.find((report) => report.slug === "product-profitability")?.columns.map((column) => column.label)).toContain("Brüt kâr");
  });
});
