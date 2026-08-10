import { describe, expect, it } from "vitest";
import { availableSettingsOptions, settingsGroups } from "./catalog";

describe("settings catalog", () => {
  // Burada yalnızca uygulanmış ve gerçek rotası bulunan ayar seçeneklerinin etkin olduğunu doğruluyorum.
  it("exposes only implemented settings destinations", () => {
    expect(availableSettingsOptions.map((option) => [option.title, option.href])).toEqual([
      ["Kargo yöntemleri", "/settings/shipping-methods"],
      ["Koleksiyonlar", "/collections"],
      ["Markalar", "/brands"],
      ["Katalog tanımları", "/settings/catalog/product-types"],
      ["Vergi oranları", "/settings/tax-rates"],
      ["Hesabım", "/settings/account"],
      ["Yöneticiler", "/managers"],
      ["Oturumlar ve güvenlik", "/settings/security"],
    ]);
  });

  // Burada geliştirme aşamasındaki seçeneklerin tıklanabilir sahte bir rotaya sahip olmadığını doğruluyorum.
  it("keeps in-development options non-navigable", () => {
    const inDevelopment = settingsGroups.flatMap((group) => group.options).filter((option) => option.status === "in-development");
    expect(inDevelopment.length).toBeGreaterThan(0);
    expect(inDevelopment.every((option) => option.href === undefined)).toBe(true);
  });
});
