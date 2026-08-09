import { afterEach, describe, expect, it, vi } from "vitest";
import { uploadProductImage, validateProductImageFile } from "./cloudinary-upload";

describe("product image validation", () => {
  // Burada desteklenen dosya türü ve sekiz MB sınırındaki bir görselin kabul edildiğini doğruluyorum.
  it("accepts JPG, PNG and WebP files within 8 MB", () => {
    expect(validateProductImageFile({ type: "image/jpeg", size: 1024 })).toBeNull();
    expect(validateProductImageFile({ type: "image/png", size: 8 * 1024 * 1024 })).toBeNull();
    expect(validateProductImageFile({ type: "image/webp", size: 2048 })).toBeNull();
  });

  // Burada SVG, boş ve sınırı aşan dosyaların yükleme öncesi durdurulduğunu doğruluyorum.
  it("rejects unsupported, empty and oversized files", () => {
    expect(validateProductImageFile({ type: "image/svg+xml", size: 1024 })).toContain("JPG");
    expect(validateProductImageFile({ type: "image/png", size: 0 })).toContain("Boş");
    expect(validateProductImageFile({ type: "image/webp", size: 8 * 1024 * 1024 + 1 })).toContain("8 MB");
  });
});

describe("uploadProductImage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    delete process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME;
    delete process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET;
  });

  // Burada unsigned preset ve ürün klasörünün Cloudinary isteğine doğru eklendiğini doğruluyorum.
  it("uploads directly into the public product folder", async () => {
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME = "demo";
    process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET = "admin-products";
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      secure_url: "https://res.cloudinary.com/demo/image/upload/v123/products/P00001/rug.webp",
      public_id: "products/P00001/rug",
    }), { status: 200, headers: { "content-type": "application/json" } }));
    vi.stubGlobal("fetch", fetchMock);

    const file = new File(["image"], "rug.webp", { type: "image/webp" });
    const asset = await uploadProductImage({ key: "local-1", file }, "P00001");

    expect(asset).toEqual({
      clientKey: "local-1",
      imageUrl: "https://res.cloudinary.com/demo/image/upload/v123/products/P00001/rug.webp",
      publicId: "products/P00001/rug",
    });
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("https://api.cloudinary.com/v1_1/demo/image/upload");
    expect((init.body as FormData).get("upload_preset")).toBe("admin-products");
    expect((init.body as FormData).get("folder")).toBe("products/P00001");
  });

  // Burada preset klasörü isteği ezdiğinde dönen beklenmeyen public id'nin kaydedilmediğini doğruluyorum.
  it("rejects a response outside the expected product folder", async () => {
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME = "demo";
    process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET = "admin-products";
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({
      secure_url: "https://res.cloudinary.com/demo/image/upload/v123/wrong/rug.webp",
      public_id: "wrong/rug",
    }), { status: 200 })));

    const file = new File(["image"], "rug.webp", { type: "image/webp" });
    await expect(uploadProductImage({ key: "local-1", file }, "P00001")).rejects.toThrow("yanıtı doğrulanamadı");
  });
});
