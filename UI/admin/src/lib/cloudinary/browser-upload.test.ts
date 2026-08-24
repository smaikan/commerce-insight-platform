import { afterEach, describe, expect, it, vi } from "vitest";
import {
  CLOUDINARY_UPLOAD_TIMEOUT_MS,
  isTrustedCloudinaryAsset,
  uploadCloudinaryAsset,
  validateBannerFile,
  validateImageFile,
} from "./browser-upload";

describe("Cloudinary dosya doğrulaması", () => {
  // Burada ortak görsel türlerini ve sekiz MB sınırını doğruluyorum.
  it("görsel dosya kurallarını uygular", () => {
    expect(validateImageFile({ type: "image/webp", size: 1024 })).toBeNull();
    expect(validateImageFile({ type: "image/svg+xml", size: 1024 })).toContain("JPG");
    expect(validateImageFile({ type: "image/png", size: 8 * 1024 * 1024 + 1 })).toContain("8 MB");
  });

  // Burada banner videolarını yalnız MP4 ve WebM ile yirmi beş MB altında kabul ediyorum.
  it("banner video kurallarını uygular", () => {
    expect(validateBannerFile({ type: "video/mp4", size: 1024 })).toBeNull();
    expect(validateBannerFile({ type: "video/webm", size: 25 * 1024 * 1024 })).toBeNull();
    expect(validateBannerFile({ type: "video/quicktime", size: 1024 })).toContain("MP4");
    expect(validateBannerFile({ type: "video/mp4", size: 25 * 1024 * 1024 + 1 })).toContain("25 MB");
  });
});

describe("Cloudinary güvenli yükleme", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    delete process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME;
    delete process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET;
    delete process.env.NEXT_PUBLIC_CLOUDINARY_BANNER_UPLOAD_PRESET;
  });

  // Burada doğru hesap ve klasörden dönen güvenli URL'nin kabul edildiğini doğruluyorum.
  it("doğrulanmış varlığı döndürür", async () => {
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME = "demo";
    process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET = "admin-images";
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({
      secure_url: "https://res.cloudinary.com/demo/image/upload/v1/brands/123/logo.webp",
      public_id: "brands/123/logo",
      resource_type: "image",
    }), { status: 200 })));

    const file = new File(["image"], "logo.webp", { type: "image/webp" });
    await expect(uploadCloudinaryAsset({ file, folder: "brands/123" })).resolves.toMatchObject({
      secureUrl: "https://res.cloudinary.com/demo/image/upload/v1/brands/123/logo.webp",
      resourceType: "image",
    });
  });

  // Burada Cloudinary bağlantısı cevap vermediğinde formun sonsuza kadar beklemek yerine yeniden denenebilir hataya geçtiğini doğruluyorum.
  it("yükleme zaman aşımını güvenli ve anlaşılır hataya dönüştürür", async () => {
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME = "demo";
    process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET = "admin-images";
    const timeoutController = new AbortController();
    const timeoutSpy = vi.spyOn(AbortSignal, "timeout").mockReturnValue(timeoutController.signal);
    vi.stubGlobal("fetch", vi.fn((_url: string | URL | Request, init?: RequestInit) => new Promise<Response>((_resolve, reject) => {
      init?.signal?.addEventListener("abort", () => reject(init.signal?.reason), { once: true });
    })));

    const file = new File(["image"], "logo.webp", { type: "image/webp" });
    const upload = uploadCloudinaryAsset({ file, folder: "brands/123" });
    timeoutController.abort(new DOMException("Timed out", "TimeoutError"));

    await expect(upload).rejects.toThrow("zaman aşımına uğradı");
    expect(timeoutSpy).toHaveBeenCalledWith(CLOUDINARY_UPLOAD_TIMEOUT_MS);
  });

  // Burada farklı Cloudinary klasöründen gelen bir varlığın güvenilir sayılmadığını doğruluyorum.
  it("beklenmeyen klasörü reddeder", () => {
    expect(isTrustedCloudinaryAsset({
      secureUrl: "https://res.cloudinary.com/demo/image/upload/v1/wrong/logo.webp",
      publicId: "wrong/logo",
      resourceType: "image",
    }, { folder: "brands/123", cloudName: "demo" })).toBe(false);
  });

  // Burada ayrı banner presetinin ürün görsel presetine ihtiyaç duymadan video yükleyebildiğini doğruluyorum.
  it("açıkça verilen banner presetiyle video yükler", async () => {
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME = "demo";
    process.env.NEXT_PUBLIC_CLOUDINARY_BANNER_UPLOAD_PRESET = "admin-banners";
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({
      secure_url: "https://res.cloudinary.com/demo/video/upload/v1/banners/main-banner/hero.mp4",
      public_id: "banners/main-banner/hero",
      resource_type: "video",
    }), { status: 200 })));

    const file = new File(["video"], "hero.mp4", { type: "video/mp4" });
    await expect(uploadCloudinaryAsset({
      file,
      folder: "banners/main-banner",
      uploadPreset: process.env.NEXT_PUBLIC_CLOUDINARY_BANNER_UPLOAD_PRESET,
      resourceType: "auto",
    })).resolves.toMatchObject({ resourceType: "video" });
  });
});
