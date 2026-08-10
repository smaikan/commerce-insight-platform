import { afterEach, describe, expect, it } from "vitest";
import { isTrustedCollectionImageAsset, parseCollectionForm } from "./form-data";

function baseFormData(): FormData {
  const formData = new FormData();
  formData.set("name", "Yaz Koleksiyonu");
  formData.set("url", "yaz-koleksiyonu");
  formData.set("description", "Sezon seçkisi");
  formData.set("displayOrder", "2");
  return formData;
}

describe("collection form data", () => {
  afterEach(() => {
    delete process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME;
  });

  // Burada görsel değişmediğinde temel koleksiyon alanlarının ve koruma niyetinin ayrı taşındığını doğruluyorum.
  it("parses collection fields while keeping the existing image", () => {
    const parsed = parseCollectionForm(baseFormData());
    expect(parsed).toMatchObject({
      ok: true,
      imageMode: "keep",
      value: {
        name: "Yaz Koleksiyonu",
        url: "yaz-koleksiyonu",
        description: "Sezon seçkisi",
        displayOrder: 2,
        imageUrl: null,
      },
    });
  });

  // Burada görsel kaldırma niyetinin yeni bir Cloudinary varlığı gerektirmeden kabul edildiğini doğruluyorum.
  it("accepts removing the existing image", () => {
    const formData = baseFormData();
    formData.set("imageMode", "remove");
    const parsed = parseCollectionForm(formData);
    expect(parsed).toMatchObject({ ok: true, imageMode: "remove" });
  });

  // Burada yalnız doğru Cloudinary hesabı ve koleksiyon klasöründeki görselin değiştirme için kabul edildiğini doğruluyorum.
  it("accepts a trusted Cloudinary collection image", () => {
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME = "demo";
    const formData = baseFormData();
    formData.set("imageMode", "replace");
    formData.set("imageUrl", "https://res.cloudinary.com/demo/image/upload/v1/collections/abc/cover.webp");
    formData.set("imagePublicId", "collections/abc/cover");
    const parsed = parseCollectionForm(formData);
    expect(parsed).toMatchObject({
      ok: true,
      imageMode: "replace",
      imageAsset: { publicId: "collections/abc/cover", resourceType: "image" },
    });
    expect(isTrustedCollectionImageAsset(parsed.ok ? parsed.imageAsset : undefined, "abc")).toBe(true);
  });

  // Burada koleksiyon klasörü dışındaki veya eksik Cloudinary yanıtının forma girmediğini doğruluyorum.
  it("rejects an untrusted replacement image", () => {
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME = "demo";
    const formData = baseFormData();
    formData.set("imageMode", "replace");
    formData.set("imageUrl", "https://res.cloudinary.com/demo/image/upload/v1/products/P00001/cover.webp");
    formData.set("imagePublicId", "products/P00001/cover");
    const parsed = parseCollectionForm(formData);
    expect(parsed.ok).toBe(false);
    if (!parsed.ok) expect(parsed.state.fieldErrors?.imageUrl).toBeDefined();
  });
});
