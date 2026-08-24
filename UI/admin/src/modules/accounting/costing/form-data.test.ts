import { describe, expect, it } from "vitest";
import { parseOpeningCostForm } from "./form-data";

const layerId = "34343434-3434-4343-8343-343434343434";
const variantId = "77777777-7777-4777-8777-777777777777";
const token = "35353535-3535-4353-8353-353535353535";

// Burada açılış maliyeti formunun kuruş hassasiyetini ve kimlik sınırlarını doğruluyorum.
describe("opening cost form", () => {
  it("parses valid comma decimals without losing the concurrency token", () => {
    const form = new FormData();
    form.set("layerId", layerId); form.set("productVariantId", variantId); form.set("expectedConcurrencyToken", token); form.set("unitCostExcludingVat", "81,25"); form.set("unitCostIncludingVat", "97.50");
    expect(parseOpeningCostForm(form)).toEqual({ ok: true, draft: { layerId, productVariantId: variantId, expectedConcurrencyToken: token, unitCostExcludingVat: "81,25", unitCostIncludingVat: "97.50" }, input: { expectedConcurrencyToken: token, unitCostExcludingVat: 81.25, unitCostIncludingVat: 97.5 } });
  });

  it("rejects negative and sub-cent values", () => {
    const form = new FormData();
    form.set("layerId", layerId); form.set("productVariantId", variantId); form.set("expectedConcurrencyToken", token); form.set("unitCostExcludingVat", "-1"); form.set("unitCostIncludingVat", "10.001");
    const result = parseOpeningCostForm(form);
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.state.fieldErrors).toMatchObject({ unitCostExcludingVat: expect.any(Array), unitCostIncludingVat: expect.any(Array) });
  });
});
