import { describe, expect, it } from "vitest";
import { getMovementFieldError, parseBulkMovements } from "./stock-movement-form-data";

describe("SKU tabanlı stok hareketi form verisi", () => {
  // Burada SKU'nun temizlenip yönün imzalı miktara dönüştürüldüğünü doğruluyorum.
  it("geçerli satırı API sözleşmesine dönüştürür", () => {
    const result = parseBulkMovements(JSON.stringify([
      {
        productVariantSku: "  SKU-RED-M  ",
        type: 10,
        direction: 1,
        quantity: "4",
        reason: "  Mal kabul  ",
      },
    ]));

    expect(result).toEqual({
      ok: true,
      movements: [{
        productVariantSku: "SKU-RED-M",
        type: 10,
        quantityDelta: 4,
        reason: "Mal kabul",
      }],
    });
  });

  // Burada eski UUID alanının SKU yerine sessizce kabul edilmesini engelliyorum.
  it("productVariantSku bulunmayan eski satırı reddeder", () => {
    const result = parseBulkMovements(JSON.stringify([
      {
        productVariantId: "11111111-1111-1111-1111-111111111111",
        type: 10,
        direction: 1,
        quantity: "1",
        reason: "",
      },
    ]));

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.state.message).toContain("Varyant SKU zorunludur");
    expect(getMovementFieldError(result.state.fieldErrors, 0, "productVariantSku"))
      .toBe("Varyant SKU zorunludur.");
  });

  // Burada stok çıkış yönünün API'ye negatif miktar olarak gönderildiğini doğruluyorum.
  it("çıkış yönünü negatif miktara dönüştürür", () => {
    const result = parseBulkMovements(JSON.stringify([
      {
        productVariantSku: "SKU-DAMAGED",
        type: 41,
        direction: 2,
        quantity: "2",
        reason: "Hasarlı ürün",
      },
    ]));

    expect(result).toMatchObject({
      ok: true,
      movements: [{ productVariantSku: "SKU-DAMAGED", quantityDelta: -2 }],
    });
  });
});
