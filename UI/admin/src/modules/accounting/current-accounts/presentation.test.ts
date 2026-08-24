import { describe, expect, it } from "vitest";
import { formatAccountingDate, formatAccountingMoney } from "../core/presentation";
import { currentAccountTypeLabel } from "./presentation";
describe("accounting presentation", () => {
  it("maps numeric current account types with a safe drift fallback", () => { expect([1, 2, 3, 99].map(currentAccountTypeLabel)).toEqual(["Müşteri", "Tedarikçi", "Müşteri ve tedarikçi", "Bilinmeyen cari türü"]); });
  it("formats authoritative money and safe dates", () => { expect(formatAccountingMoney(1234567.5)).toContain("1.234.567,50"); expect(formatAccountingDate(null)).toBe("—"); expect(formatAccountingDate("invalid")).toBe("—"); });
});
