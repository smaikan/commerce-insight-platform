import { describe, expect, it } from "vitest";
import { isTrustedCloudinaryProductAsset } from "./product-media";

describe("isTrustedCloudinaryProductAsset", () => {
  // Burada yalnız doğru Cloudinary hesabı ve ürün klasörü birleşiminin kabul edildiğini doğruluyorum.
  it("accepts the configured account and exact product folder", () => {
    expect(isTrustedCloudinaryProductAsset({
      imageUrl: "https://res.cloudinary.com/demo/image/upload/v123/products/P00001/rug.webp",
      publicId: "products/P00001/rug",
    }, "P00001", "demo")).toBe(true);
  });

  // Burada başka ürün klasöründen veya Cloudinary dışından gelen URL'nin reddedildiğini doğruluyorum.
  it("rejects another product folder and external hosts", () => {
    expect(isTrustedCloudinaryProductAsset({
      imageUrl: "https://res.cloudinary.com/demo/image/upload/v123/products/P00002/rug.webp",
      publicId: "products/P00002/rug",
    }, "P00001", "demo")).toBe(false);
    expect(isTrustedCloudinaryProductAsset({
      imageUrl: "https://example.com/demo/image/upload/products/P00001/rug.webp",
      publicId: "products/P00001/rug",
    }, "P00001", "demo")).toBe(false);
    expect(isTrustedCloudinaryProductAsset({
      imageUrl: "https://res.cloudinary.com/demo/image/upload/v123/products/P00002/rug.webp",
      publicId: "products/P00001/rug",
    }, "P00001", "demo")).toBe(false);
  });
});
