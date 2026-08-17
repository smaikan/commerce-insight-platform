import { afterEach, describe, expect, it, vi } from "vitest";
import { replaceStoreSettingsMedia } from "./media-client";

describe("StoreSettings unsigned Cloudinary yüklemesi", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    delete process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME;
    delete process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET;
  });

  // Burada yeni logo dosyasının unsigned preset ile doğru klasöre eklenip URL'sinin döndüğünü doğruluyorum.
  it("Cloudinary secure URL sonucunu StoreSettings taslağına döndürür", async () => {
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME = "demo";
    process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET = "admin-images";
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      secure_url: "https://res.cloudinary.com/demo/image/upload/v1/store-settings/logo/new-logo.webp",
      public_id: "store-settings/logo/new-logo",
      resource_type: "image",
    }), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);

    const file = new File(["image"], "new-logo.webp", { type: "image/webp" });
    await expect(replaceStoreSettingsMedia("logo", file)).resolves.toMatchObject({
      secureUrl: "https://res.cloudinary.com/demo/image/upload/v1/store-settings/logo/new-logo.webp",
      publicId: "store-settings/logo/new-logo",
    });

    const body = fetchMock.mock.calls[0]?.[1]?.body as FormData;
    expect(body.get("upload_preset")).toBe("admin-images");
    expect(body.get("folder")).toBe("store-settings/logo");
  });
});
