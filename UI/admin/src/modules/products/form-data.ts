import type { CreateProductInput, ProductImageInput, ProductVariantInput, UpdateProductInput } from "@/modules/products/api";

type ProductFormValue = {
  productId?: string;
  base: CreateProductInput | UpdateProductInput;
  baseChanged: boolean;
  status: 0 | 1 | 2 | 3;
  isFeatured: boolean;
  hasVariants: boolean;
  originalStatus?: number;
  originalIsFeatured?: boolean;
  originalHasVariants?: boolean;
  variants: ProductVariantInput[];
  image?: ProductImageInput;
};

export type ProductFormParseResult =
  | { ok: true; value: ProductFormValue }
  | { ok: false; fieldErrors: Record<string, string[]>; message: string };

// Burada ürün formunu API'nin alan sınırlarına göre doğrulayıp sayısal wire değerlerine dönüştürüyorum.
export function parseProductForm(formData: FormData, mode: "create" | "edit"): ProductFormParseResult {
  const fieldErrors: Record<string, string[]> = {};
  const title = requiredText(formData, "title", "Ürün başlığı zorunludur.", 250, fieldErrors);
  const mainSku = requiredText(formData, "mainSku", "Ana SKU zorunludur.", 100, fieldErrors);
  const type = optionalText(formData, "type", 150, fieldErrors);
  const url = optionalText(formData, "url", 250, fieldErrors);
  const description = optionalText(formData, "description", 4000, fieldErrors);
  const seoTitle = optionalText(formData, "seoTitle", 250, fieldErrors);
  const seoDescription = optionalText(formData, "seoDescription", 500, fieldErrors);
  const brandId = optionalUuid(formData, "brandId", fieldErrors);
  const taxRateId = optionalUuid(formData, "taxRateId", fieldErrors);
  const displayOrder = integer(formData, "displayOrder", 0, 0, Number.MAX_SAFE_INTEGER, fieldErrors);
  const status = enumInteger(formData, "status", [0, 1, 2, 3] as const, fieldErrors);
  const isFeatured = checkbox(formData, "isFeatured");
  const hasVariants = checkbox(formData, "hasVariants");
  const tags = repeatedValues(formData, "tags", 20, 150, fieldErrors);
  const collections = commaSeparated(formData, "collections", 100, 150, fieldErrors);
  const parsedVariants = parseVariants(formData, fieldErrors);
  const variants = mode === "edit"
    ? parsedVariants.filter((variant) => !variant.id || variant.changed).map(withoutChangedFlag)
    : parsedVariants.map(withoutChangedFlag);
  validateUniqueVariantCombinations(parsedVariants, fieldErrors);
  const image = parseImage(formData, fieldErrors);

  if (!hasVariants && variants.length > 1) {
    addError(fieldErrors, "hasVariants", "Birden fazla varyant için varyantlı ürün seçeneğini açmalısınız.");
  }

  if (Object.keys(fieldErrors).length > 0 || status === undefined) {
    return { ok: false, fieldErrors, message: "Formdaki işaretli alanları kontrol edin." };
  }

  const common = {
    title,
    mainSku,
    type,
    url,
    brandId,
    description,
    displayOrder,
    seoTitle,
    seoDescription,
    tags,
    taxRateId,
  };

  return {
    ok: true,
    value: {
      productId: mode === "edit" ? text(formData, "productId") : undefined,
      base:
        mode === "create"
          ? {
              ...common,
              hasVariants,
              status,
              isFeatured,
              collections,
              ...(variants.length > 0 ? { variants } : {}),
            }
          : common,
      baseChanged: mode === "create" || checkbox(formData, "baseChanged"),
      status,
      isFeatured,
      hasVariants,
      originalStatus: optionalInteger(formData, "originalStatus"),
      originalIsFeatured: optionalBoolean(formData, "originalIsFeatured"),
      originalHasVariants: optionalBoolean(formData, "originalHasVariants"),
      variants,
      image,
    },
  };
}

type ParsedProductVariant = ProductVariantInput & { changed: boolean };

// Burada tüm varyantları doğrularken edit mutation listesi için kullanıcının gerçek değişiklik niyetini de taşıyorum.
function parseVariants(formData: FormData, errors: Record<string, string[]>): ParsedProductVariant[] {
  const count = integer(formData, "variantCount", 0, 0, 50, errors);
  const variants: ParsedProductVariant[] = [];

  for (let index = 0; index < count; index += 1) {
    const prefix = `variants.${index}`;
    const id = optionalText(formData, `${prefix}.id`, 36, errors);
    if (!id && isBlankNewVariant(formData, prefix)) continue;

    const name = requiredText(formData, `${prefix}.name`, "Varyant adı zorunludur.", 150, errors);
    const value = requiredText(formData, `${prefix}.value`, "Varyant değeri zorunludur.", 150, errors);
    validateCompositeOption(name, value, prefix, errors);
    const sku = requiredText(formData, `${prefix}.sku`, "Varyant SKU zorunludur.", 100, errors);
    const price = decimal(formData, `${prefix}.price`, 0.01, errors);
    const stock = integer(formData, `${prefix}.stock`, 0, 0, Number.MAX_SAFE_INTEGER, errors);
    const compareAtPrice = optionalDecimal(formData, `${prefix}.compareAtPrice`, errors);
    if (compareAtPrice !== undefined && compareAtPrice < price) {
      addError(errors, `${prefix}.compareAtPrice`, "Karşılaştırma fiyatı satış fiyatından düşük olamaz.");
    }

    variants.push({
      id,
      name,
      value,
      sku,
      price,
      stock,
      compareAtPrice,
      barcode: optionalText(formData, `${prefix}.barcode`, 100, errors),
      material: optionalText(formData, `${prefix}.material`, 120, errors),
      isActive: checkbox(formData, `${prefix}.isActive`),
      openingUnitCostExcludingVat: optionalDecimal(formData, `${prefix}.openingUnitCostExcludingVat`, errors),
      openingUnitCostIncludingVat: optionalDecimal(formData, `${prefix}.openingUnitCostIncludingVat`, errors),
      stockAdjustmentReason: optionalText(formData, `${prefix}.stockAdjustmentReason`, 500, errors),
      changed: checkbox(formData, `${prefix}.changed`),
    });
  }

  return variants;
}

// Burada yalnız UI değişiklik niyeti için kullanılan alanı backend varyant gövdesinden çıkarıyorum.
function withoutChangedFlag({ changed, ...variant }: ParsedProductVariant): ProductVariantInput {
  void changed;
  return variant;
}

// Burada yalnız otomatik üretilmiş fakat hiçbir satış detayı girilmemiş yeni varyant satırlarını payload dışında bırakıyorum.
function isBlankNewVariant(formData: FormData, prefix: string): boolean {
  const stock = text(formData, `${prefix}.stock`).trim();
  const detailFields = [
    "sku",
    "price",
    "compareAtPrice",
    "barcode",
    "material",
    "openingUnitCostExcludingVat",
    "openingUnitCostIncludingVat",
    "stockAdjustmentReason",
  ];
  return detailFields.every((field) => !text(formData, `${prefix}.${field}`).trim()) && (!stock || Number(stock) === 0);
}

// Burada birleşik varyant ad/değerlerini backend ile aynı 1-3 parça, eşit sıra ve benzersiz ad kurallarıyla doğruluyorum.
function validateCompositeOption(
  name: string,
  value: string,
  prefix: string,
  errors: Record<string, string[]>,
): void {
  const names = name.split("/").map((part) => part.trim());
  const values = value.split("/").map((part) => part.trim());
  if (names.length < 1 || names.length > 3) {
    addError(errors, `${prefix}.name`, "Bir varyant en fazla üç seçenek adı içerebilir.");
  }
  if (names.length !== values.length) {
    addError(errors, `${prefix}.value`, "Seçenek adı ve değeri aynı sayıda parçadan oluşmalıdır.");
  }
  if (names.some((part) => !part) || values.some((part) => !part)) {
    addError(errors, `${prefix}.value`, "Seçenek adı ve değerlerinde boş parça bırakılamaz.");
  }
  if (new Set(names).size !== names.length) {
    addError(errors, `${prefix}.name`, "Seçenek adları aynı varyantta tekrar edemez.");
  }
}

// Burada aynı birleşik seçenek kombinasyonunun ürün içinde birden fazla varyant satırına dönüşmesini engelliyorum.
function validateUniqueVariantCombinations(
  variants: ProductVariantInput[],
  errors: Record<string, string[]>,
): void {
  const identities = variants.map((variant) => `${variant.name.trim()}\u0000${variant.value.trim()}`);
  identities.forEach((identity, index) => {
    if (identities.indexOf(identity) !== index) {
      addError(errors, `variants.${index}.value`, "Aynı seçenek kombinasyonu tekrar eklenemez.");
    }
  });
}

// Burada isteğe bağlı URL tabanlı görsel alanlarını ayrı ürün görseli işlemine hazırlıyorum.
function parseImage(formData: FormData, errors: Record<string, string[]>): ProductImageInput | undefined {
  const imageUrl = optionalText(formData, "imageUrl", 500, errors);
  if (!imageUrl) return undefined;

  try {
    const parsed = new URL(imageUrl);
    if (parsed.protocol !== "http:" && parsed.protocol !== "https:") throw new Error("invalid protocol");
  } catch {
    addError(errors, "imageUrl", "Görsel adresi geçerli bir http veya https URL olmalıdır.");
  }

  return {
    imageUrl,
    altText: optionalText(formData, "imageAltText", 250, errors),
    displayOrder: integer(formData, "imageDisplayOrder", 0, 0, Number.MAX_SAFE_INTEGER, errors),
    isMain: checkbox(formData, "imageIsMain"),
  };
}

// Burada virgülle ayrılan organizasyon alanlarını tekrar etmeyen temiz adlara dönüştürüyorum.
function commaSeparated(
  formData: FormData,
  name: string,
  maxItems: number,
  maxLength: number,
  errors: Record<string, string[]>,
): string[] {
  const values = [...new Set(text(formData, name).split(",").map((item) => item.trim()).filter(Boolean))];
  if (values.length > maxItems) addError(errors, name, `En fazla ${maxItems} değer girebilirsiniz.`);
  if (values.some((item) => item.length > maxLength)) addError(errors, name, `Her değer en fazla ${maxLength} karakter olabilir.`);
  return values;
}

// Burada tag editörünün ayrı ayrı gönderdiği aynı adlı form alanlarını virgüle göre bölmeden temiz ve benzersiz bir diziye dönüştürüyorum.
function repeatedValues(
  formData: FormData,
  name: string,
  maxItems: number,
  maxLength: number,
  errors: Record<string, string[]>,
): string[] {
  const values = [
    ...new Set(
      formData
        .getAll(name)
        .filter((value): value is string => typeof value === "string")
        .map((value) => value.trim())
        .filter(Boolean),
    ),
  ];
  if (values.length > maxItems) addError(errors, name, `En fazla ${maxItems} değer girebilirsiniz.`);
  if (values.some((item) => item.length > maxLength)) addError(errors, name, `Her değer en fazla ${maxLength} karakter olabilir.`);
  return values;
}

// Burada zorunlu metin alanlarında boşluk ve uzunluk hatalarını aynı biçimde topluyorum.
function requiredText(
  formData: FormData,
  name: string,
  requiredMessage: string,
  maxLength: number,
  errors: Record<string, string[]>,
): string {
  const value = text(formData, name).trim();
  if (!value) addError(errors, name, requiredMessage);
  if (value.length > maxLength) addError(errors, name, `En fazla ${maxLength} karakter girebilirsiniz.`);
  return value;
}

// Burada isteğe bağlı metni boşsa undefined, doluysa uzunluk denetimli olarak döndürüyorum.
function optionalText(formData: FormData, name: string, maxLength: number, errors: Record<string, string[]>): string | undefined {
  const value = text(formData, name).trim();
  if (value.length > maxLength) addError(errors, name, `En fazla ${maxLength} karakter girebilirsiniz.`);
  return value || undefined;
}

// Burada UUID seçimlerinin istemci tarafından bozulmuş olma ihtimalini sunucu sınırında kontrol ediyorum.
function optionalUuid(formData: FormData, name: string, errors: Record<string, string[]>): string | undefined {
  const value = text(formData, name).trim();
  if (value && !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value)) {
    addError(errors, name, "Geçerli bir seçim yapın.");
  }
  return value || undefined;
}

// Burada tam sayı alanlarını belirtilen aralık içinde doğruluyorum.
function integer(
  formData: FormData,
  name: string,
  fallback: number,
  min: number,
  max: number,
  errors: Record<string, string[]>,
): number {
  const value = Number(text(formData, name));
  if (!Number.isInteger(value) || value < min || value > max) {
    addError(errors, name, `Değer ${min} ile ${max} arasında tam sayı olmalıdır.`);
    return fallback;
  }
  return value;
}

// Burada fiyat alanını nokta veya virgül ondalık ayırıcıyla pozitif sayıya dönüştürüyorum.
function decimal(formData: FormData, name: string, min: number, errors: Record<string, string[]>): number {
  const value = Number(text(formData, name).replace(",", "."));
  if (!Number.isFinite(value) || value < min) {
    addError(errors, name, `Değer en az ${min} olmalıdır.`);
    return min;
  }
  return value;
}

// Burada boş bırakılabilen parasal alanları sayı olduğunda doğruluyorum.
function optionalDecimal(formData: FormData, name: string, errors: Record<string, string[]>): number | undefined {
  const raw = text(formData, name).trim();
  if (!raw) return undefined;
  const value = Number(raw.replace(",", "."));
  if (!Number.isFinite(value) || value < 0) {
    addError(errors, name, "Sıfır veya daha büyük bir sayı girin.");
    return undefined;
  }
  return value;
}

// Burada numeric enum alanını yalnız izin verilen wire değerlerinden kabul ediyorum.
function enumInteger<T extends number>(
  formData: FormData,
  name: string,
  allowed: readonly T[],
  errors: Record<string, string[]>,
): T | undefined {
  const value = Number(text(formData, name));
  if (!allowed.includes(value as T)) {
    addError(errors, name, "Geçerli bir durum seçin.");
    return undefined;
  }
  return value as T;
}

// Burada gizli karşılaştırma alanlarındaki isteğe bağlı tam sayıyı okuyorum.
function optionalInteger(formData: FormData, name: string): number | undefined {
  const value = Number(text(formData, name));
  return Number.isInteger(value) ? value : undefined;
}

// Burada checkbox alanlarını HTML form gönderim biçiminden boolean değere dönüştürüyorum.
function checkbox(formData: FormData, name: string): boolean {
  return formData.get(name) === "on";
}

// Burada gizli boolean alanlarını yalnız açıkça true/false olduğunda kabul ediyorum.
function optionalBoolean(formData: FormData, name: string): boolean | undefined {
  const value = text(formData, name);
  if (value === "true") return true;
  if (value === "false") return false;
  return undefined;
}

// Burada FormData içindeki tekil string alanlarını güvenli boş varsayılanla okuyorum.
function text(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value : "";
}

// Burada aynı alana ait birden fazla doğrulama mesajını kaybetmeden biriktiriyorum.
function addError(errors: Record<string, string[]>, name: string, message: string): void {
  errors[name] = [...(errors[name] || []), message];
}
