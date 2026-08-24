export const MAX_IMAGE_BYTES = 8 * 1024 * 1024;
export const MAX_BANNER_VIDEO_BYTES = 25 * 1024 * 1024;
export const CLOUDINARY_UPLOAD_TIMEOUT_MS = 45_000;
export const ACCEPTED_IMAGE_TYPES = ["image/jpeg", "image/png", "image/webp"] as const;
export const ACCEPTED_BANNER_VIDEO_TYPES = ["video/mp4", "video/webm"] as const;

export type CloudinaryResourceType = "image" | "video";

export type CloudinaryAsset = {
  secureUrl: string;
  publicId: string;
  resourceType: CloudinaryResourceType;
};

type CloudinaryUploadResponse = {
  secure_url?: unknown;
  public_id?: unknown;
  resource_type?: unknown;
};

type CloudinaryUploadInput = {
  file: File;
  folder: string;
  uploadPreset?: string;
  resourceType?: "image" | "auto";
  tags?: string[];
};

type TrustedAssetOptions = {
  folder: string;
  cloudName?: string;
  allowedResourceTypes?: CloudinaryResourceType[];
};

// Burada tarayıcıya açık Cloudinary hesap ve varsayılan görsel preset bilgilerini okuyorum.
export function getCloudinaryBrowserConfig(): { cloudName: string; uploadPreset: string } {
  const cloudName = process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME?.trim();
  const uploadPreset = process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET?.trim();
  if (!cloudName || !uploadPreset) {
    throw new Error("Görsel yükleme hizmeti yapılandırılmamış. Yöneticiyle iletişime geçin.");
  }
  return { cloudName, uploadPreset };
}

// Burada banner yüklemelerini ürün görseli kurallarından ayıran public preset değerini okuyorum.
export function getCloudinaryBannerUploadPreset(): string {
  const uploadPreset = process.env.NEXT_PUBLIC_CLOUDINARY_BANNER_UPLOAD_PRESET?.trim();
  if (!uploadPreset) {
    throw new Error("Banner yükleme hizmeti yapılandırılmamış. Yöneticiyle iletişime geçin.");
  }
  return uploadPreset;
}

// Burada marka, koleksiyon ve ürün görsellerini ortak dosya kurallarıyla doğruluyorum.
export function validateImageFile(file: Pick<File, "type" | "size">): string | null {
  if (!ACCEPTED_IMAGE_TYPES.includes(file.type as (typeof ACCEPTED_IMAGE_TYPES)[number])) {
    return "Yalnızca JPG, PNG veya WebP dosyaları yüklenebilir.";
  }
  if (file.size === 0) return "Boş görsel dosyası yüklenemez.";
  if (file.size > MAX_IMAGE_BYTES) return "Görsel en fazla 8 MB olabilir.";
  return null;
}

// Burada banner dosyasını görsel ve video için belirlenen ayrı boyut sınırlarında doğruluyorum.
export function validateBannerFile(file: Pick<File, "type" | "size">): string | null {
  if (ACCEPTED_IMAGE_TYPES.includes(file.type as (typeof ACCEPTED_IMAGE_TYPES)[number])) {
    return validateImageFile(file);
  }
  if (!ACCEPTED_BANNER_VIDEO_TYPES.includes(file.type as (typeof ACCEPTED_BANNER_VIDEO_TYPES)[number])) {
    return "Banner için JPG, PNG, WebP, MP4 veya WebM dosyası yükleyin.";
  }
  if (file.size === 0) return "Boş video dosyası yüklenemez.";
  if (file.size > MAX_BANNER_VIDEO_BYTES) return "Banner videosu en fazla 25 MB olabilir.";
  return null;
}

// Burada Cloudinary yanıtının beklenen hesap, klasör ve medya türüne ait olduğunu doğruluyorum.
export function isTrustedCloudinaryAsset(asset: CloudinaryAsset, options: TrustedAssetOptions): boolean {
  const cloudName = options.cloudName || process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME?.trim();
  const normalizedFolder = options.folder.replace(/^\/+|\/+$/g, "");
  if (!cloudName || !normalizedFolder || asset.secureUrl.length > 500) return false;
  if (!asset.publicId.startsWith(`${normalizedFolder}/`)) return false;
  if (options.allowedResourceTypes && !options.allowedResourceTypes.includes(asset.resourceType)) return false;

  try {
    const url = new URL(asset.secureUrl);
    const decodedPath = decodeURIComponent(url.pathname);
    return url.protocol === "https:"
      && url.hostname === "res.cloudinary.com"
      && decodedPath.startsWith(`/${cloudName}/${asset.resourceType}/upload/`)
      && decodedPath.includes(`/${normalizedFolder}/`);
  } catch {
    return false;
  }
}

// Burada dosyayı doğrudan Cloudinary'ye yükleyip yalnız doğrulanmış güvenli varlık bilgisini döndürüyorum.
export async function uploadCloudinaryAsset(input: CloudinaryUploadInput, signal?: AbortSignal): Promise<CloudinaryAsset> {
  const cloudName = process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME?.trim();
  const uploadPreset = input.uploadPreset?.trim() || process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET?.trim();
  const normalizedFolder = input.folder.replace(/^\/+|\/+$/g, "");
  const requestedResourceType = input.resourceType || "image";
  if (!cloudName || !uploadPreset) {
    throw new Error("Medya yükleme hizmeti yapılandırılmamış. Yöneticiyle iletişime geçin.");
  }
  if (!normalizedFolder) throw new Error("Medya klasörü belirlenemedi.");

  const body = new FormData();
  body.set("file", input.file);
  body.set("upload_preset", uploadPreset);
  body.set("folder", normalizedFolder);
  if (input.tags?.length) body.set("tags", input.tags.join(","));

  const timeoutSignal = AbortSignal.timeout(CLOUDINARY_UPLOAD_TIMEOUT_MS);
  const requestSignal = signal ? AbortSignal.any([signal, timeoutSignal]) : timeoutSignal;
  let response: Response;
  let payload: CloudinaryUploadResponse | null;
  try {
    response = await fetch(
      `https://api.cloudinary.com/v1_1/${encodeURIComponent(cloudName)}/${requestedResourceType}/upload`,
      { method: "POST", body, signal: requestSignal },
    );
    payload = await response.json().catch(() => null) as CloudinaryUploadResponse | null;
  } catch {
    if (signal?.aborted) throw new Error("Medya yükleme işlemi iptal edildi.");
    if (timeoutSignal.aborted) {
      throw new Error("Medya yükleme zaman aşımına uğradı. Bağlantınızı kontrol edip yalnız eksik adımı yeniden deneyin.");
    }
    throw new Error("Medya yükleme hizmetine bağlanılamadı. Bağlantınızı kontrol edip tekrar deneyin.");
  }
  if (!response.ok) throw new Error("Medya yüklenemedi. Lütfen tekrar deneyin.");

  const asset: CloudinaryAsset = {
    secureUrl: typeof payload?.secure_url === "string" ? payload.secure_url : "",
    publicId: typeof payload?.public_id === "string" ? payload.public_id : "",
    resourceType: payload?.resource_type === "video" ? "video" : "image",
  };
  const allowedResourceTypes: CloudinaryResourceType[] = requestedResourceType === "image" ? ["image"] : ["image", "video"];
  if (!isTrustedCloudinaryAsset(asset, { folder: normalizedFolder, cloudName, allowedResourceTypes })) {
    throw new Error("Medya yükleme yanıtı doğrulanamadı. Yöneticiyle iletişime geçin.");
  }
  return asset;
}
