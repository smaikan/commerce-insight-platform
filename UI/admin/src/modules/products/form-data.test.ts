import { describe, expect, it } from "vitest";
import { parseProductForm } from "./form-data";

// Burada testlerde tekrar eden zorunlu ürün ve varyant alanlarını geçerli bir FormData gövdesiyle hazırlıyorum.
function validProductForm(): FormData {
  const form = new FormData();
  form.set("title", "Luna Küpe");
  form.set("mainSku", "LUNA-001");
  form.set("status", "0");
  form.set("displayOrder", "0");
  form.set("variantCount", "1");
  form.set("variants.0.name", "Varsayılan");
  form.set("variants.0.value", "Standart");
  form.set("variants.0.sku", "LUNA-001");
  form.set("variants.0.price", "899.90");
  form.set("variants.0.stock", "10");
  form.set("variants.0.isActive", "on");
  return form;
}

describe("product form data", () => {
  // Burada geçerli oluşturma formunun numeric enum, fiyat ve stok wire değerlerine dönüştüğünü doğruluyorum.
  it("parses a valid product with one variant", () => {
    const result = parseProductForm(validProductForm(), "create");

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.value.status).toBe(0);
      expect(result.value.variants[0]).toMatchObject({ price: 899.9, stock: 10, isActive: true });
      expect(result.value.base).not.toHaveProperty("isActive");
    }
  });

  // Burada tag editörünün aynı adla gönderdiği alanların virgül ayracı olmadan ayrı taglar olarak parse edildiğini doğruluyorum.
  it("parses tags from repeated form fields", () => {
    const form = validProductForm();
    form.append("tags", "Yeni sezon");
    form.append("tags", "Aksesuar");

    const result = parseProductForm(form, "create");
    expect(result.ok).toBe(true);
    if (result.ok) expect(result.value.base.tags).toEqual(["Yeni sezon", "Aksesuar"]);
  });

  // Burada varyant modu açıldığında birden fazla seçenek değerinin ayrı API varyantlarına dönüştüğünü doğruluyorum.
  it("parses multiple option values when variants are enabled", () => {
    const form = validProductForm();
    form.set("hasVariants", "on");
    form.set("variantCount", "2");
    form.set("variants.0.name", "Renk");
    form.set("variants.0.value", "Altın");
    form.set("variants.0.sku", "LUNA-001-ALTIN");
    form.set("variants.1.name", "Renk");
    form.set("variants.1.value", "Gümüş");
    form.set("variants.1.sku", "LUNA-001-GUMUS");
    form.set("variants.1.price", "899.90");
    form.set("variants.1.stock", "4");
    form.set("variants.1.isActive", "on");

    const result = parseProductForm(form, "create");
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.value.hasVariants).toBe(true);
      expect(result.value.variants.map((variant) => variant.value)).toEqual(["Altın", "Gümüş"]);
    }
  });

  // Burada iki seçenekli birleşik ad ve değerin backend wire biçiminde kabul edildiğini doğruluyorum.
  it("parses a composite option combination", () => {
    const form = validProductForm();
    form.set("hasVariants", "on");
    form.set("variants.0.name", "Renk / Cins");
    form.set("variants.0.value", "Siyah / Kadın");

    const result = parseProductForm(form, "create");
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.value.variants[0]).toMatchObject({ name: "Renk / Cins", value: "Siyah / Kadın" });
    }
  });

  // Burada ad ve değer parça sayısı eşleşmeyen birleşik varyantın sunucu sınırında reddedildiğini doğruluyorum.
  it("rejects mismatched composite option parts", () => {
    const form = validProductForm();
    form.set("hasVariants", "on");
    form.set("variants.0.name", "Renk / Cins");
    form.set("variants.0.value", "Siyah");

    const result = parseProductForm(form, "create");
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.fieldErrors["variants.0.value"]).toBeDefined();
  });

  // Burada aynı seçenek kombinasyonunun iki ayrı varyant satırında tekrar gönderilmesini reddediyorum.
  it("rejects duplicate option combinations", () => {
    const form = validProductForm();
    form.set("hasVariants", "on");
    form.set("variantCount", "2");
    form.set("variants.1.name", "Varsayılan");
    form.set("variants.1.value", "Standart");
    form.set("variants.1.sku", "LUNA-002");
    form.set("variants.1.price", "899.90");
    form.set("variants.1.stock", "2");

    const result = parseProductForm(form, "create");
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.fieldErrors["variants.1.value"]).toBeDefined();
  });

  // Burada adı/değeri otomatik oluşmuş ancak hiçbir satış detayı girilmemiş yeni varyantın payload'a alınmadığını doğruluyorum.
  it("omits a completely blank generated variant", () => {
    const form = validProductForm();
    form.set("hasVariants", "on");
    form.delete("variants.0.sku");
    form.delete("variants.0.price");
    form.set("variants.0.stock", "0");

    const result = parseProductForm(form, "create");
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.value.variants).toEqual([]);
      expect(result.value.base).not.toHaveProperty("variants");
    }
  });

  // Burada çaprazlanan satırlardan biri dolu diğeri tamamen boşsa yalnız dolu varyantın oluşturma payload'ına girdiğini doğruluyorum.
  it("omits only the blank row from crossed variants", () => {
    const form = validProductForm();
    form.set("hasVariants", "on");
    form.set("variantCount", "2");
    form.set("variants.0.name", "Renk");
    form.set("variants.0.value", "Altın");
    form.set("variants.1.name", "Renk");
    form.set("variants.1.value", "Gümüş");
    form.set("variants.1.stock", "0");

    const result = parseProductForm(form, "create");

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.value.variants).toHaveLength(1);
      expect(result.value.variants[0].value).toBe("Altın");
    }
  });

  // Burada kısmen doldurulan varyantın atlanmak yerine zorunlu SKU ve fiyat kurallarıyla doğrulandığını kontrol ediyorum.
  it("validates a partially filled generated variant", () => {
    const form = validProductForm();
    form.set("hasVariants", "on");
    form.delete("variants.0.sku");
    form.delete("variants.0.price");
    form.set("variants.0.barcode", "8690000000001");

    const result = parseProductForm(form, "create");
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.fieldErrors["variants.0.sku"]).toBeDefined();
      expect(result.fieldErrors["variants.0.price"]).toBeDefined();
    }
  });

  // Burada birden fazla varyantın hasVariants kapalıyken gönderilemediğini doğruluyorum.
  it("requires hasVariants for multiple variants", () => {
    const form = validProductForm();
    form.set("variantCount", "2");
    form.set("variants.1.name", "Renk");
    form.set("variants.1.value", "Gümüş");
    form.set("variants.1.sku", "LUNA-001-GUMUS");
    form.set("variants.1.price", "899.90");
    form.set("variants.1.stock", "4");
    form.set("variants.1.isActive", "on");

    const result = parseProductForm(form, "create");
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.fieldErrors.hasVariants).toBeDefined();
  });

  // Burada karşılaştırma fiyatının satış fiyatının altında olamayacağını form sınırında doğruluyorum.
  it("rejects compare-at price below the selling price", () => {
    const form = validProductForm();
    form.set("variants.0.compareAtPrice", "500");

    const result = parseProductForm(form, "create");
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.fieldErrors["variants.0.compareAtPrice"]).toBeDefined();
  });

  // Burada düzenleme ekranındaki dokunulmamış kayıtlı varyantın gereksiz update isteğine dönüşmediğini doğruluyorum.
  it("omits an unchanged persisted variant while editing", () => {
    const form = validProductForm();
    form.set("productId", "P00004");
    form.set("variants.0.id", "11111111-1111-4111-8111-111111111111");

    const result = parseProductForm(form, "edit");

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.value.baseChanged).toBe(false);
      expect(result.value.variants).toEqual([]);
    }
  });

  // Burada kullanıcı satış alanına dokunduğunda kayıtlı varyantın update listesine alındığını doğruluyorum.
  it("includes a changed persisted variant while editing", () => {
    const form = validProductForm();
    form.set("productId", "P00004");
    form.set("variants.0.id", "11111111-1111-4111-8111-111111111111");
    form.set("variants.0.changed", "on");

    const result = parseProductForm(form, "edit");

    expect(result.ok).toBe(true);
    if (result.ok) expect(result.value.variants).toHaveLength(1);
  });

  // Burada çoklu varyantlı üründe yalnız ana SKU değiştiğinde temel kaydın güncellenip dört varyantın tekrar gönderilmediğini doğruluyorum.
  it("keeps persisted variants out of an edit that only changes the main SKU", () => {
    const form = validProductForm();
    form.set("productId", "P00004");
    form.set("baseChanged", "on");
    form.set("hasVariants", "on");
    form.set("mainSku", "AUR-EAR-802");
    form.set("variantCount", "4");

    for (let index = 0; index < 4; index += 1) {
      form.set(`variants.${index}.id`, `11111111-1111-4111-8111-11111111111${index}`);
      form.set(`variants.${index}.name`, "Renk");
      form.set(`variants.${index}.value`, ["Altın", "Gümüş", "Siyah", "Beyaz"][index]);
      form.set(`variants.${index}.sku`, `AUR-EAR-001-V0${index + 1}`);
      form.set(`variants.${index}.price`, "899.90");
      form.set(`variants.${index}.stock`, "10");
      form.set(`variants.${index}.isActive`, "on");
    }

    const result = parseProductForm(form, "edit");

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.value.baseChanged).toBe(true);
      expect(result.value.base.mainSku).toBe("AUR-EAR-802");
      expect(result.value.variants).toEqual([]);
    }
  });
});
