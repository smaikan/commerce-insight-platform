import { describe, expect, it } from "vitest";
import { parseSettingsListQuery, settingsListHref } from "./query";

describe("settings list query", () => {
  // Burada geçerli sayfalama değerlerinin değişmeden kabul edildiğini doğruluyorum.
  it("parses supported values", () => {
    expect(parseSettingsListQuery({ page: "3", pageSize: "50" })).toEqual({ pageNumber: 3, pageSize: 50 });
  });

  // Burada bozuk ve API sınırını aşan değerlerin güvenli varsayılanlara döndüğünü doğruluyorum.
  it("falls back for invalid values", () => {
    expect(parseSettingsListQuery({ page: "0", pageSize: "101" })).toEqual({ pageNumber: 1, pageSize: 20 });
  });

  // Burada iki ayar listesinin de paylaşacağı sayfa bağlantısı biçimini doğruluyorum.
  it("builds a stable page link", () => {
    expect(settingsListHref("/settings/tax-rates", { pageNumber: 1, pageSize: 20 }, 2)).toBe("/settings/tax-rates?page=2&pageSize=20");
  });
});
