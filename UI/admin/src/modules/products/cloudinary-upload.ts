import {
  ACCEPTED_PRODUCT_IMAGE_TYPES,
  isTrustedCloudinaryProductAsset,
  MAX_PRODUCT_IMAGE_BYTES,
  PRODUCT_IMAGE_UPLOAD_CONCURRENCY,
  type CloudinaryProductAsset,
  type ProductMediaDraftItem,
} from "./product-media";

type CloudinaryUploadResponse = {
  secure_url?: unknown;
  public_id?: unknown;
};

export type ProductImageUploadBatch = {
  uploaded: CloudinaryProductAsset[];
  failed: Array<{ clientKey: string; fileName: string; message: string }>;
};

// Burada yalnız tarayıcıya açık Cloudinary hesap adı ve unsigned preset değerlerini okuyorum.
export function getCloudinaryBrowserConfig(): { cloudName: string; uploadPreset: string } {
  const cloudName = process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME?.trim();
  const uploadPreset = process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET?.trim();
  if (!cloudName || !uploadPreset) {
    throw new Error("Görsel yükleme hizmeti yapılandırılmamış. Yöneticiyle iletişime geçin.");
  }
  return { cloudName, uploadPreset };
}

// Burada dosya türünü ve sekiz MB sınırını yükleme başlamadan önce denetliyorum.
export function validateProductImageFile(file: Pick<File, "type" | "size">): string | null {
  if (!ACCEPTED_PRODUCT_IMAGE_TYPES.includes(file.type as (typeof ACCEPTED_PRODUCT_IMAGE_TYPES)[number])) {
    return "Yalnızca JPG, PNG veya WebP dosyaları yüklenebilir.";
  }
  if (file.size > MAX_PRODUCT_IMAGE_BYTES) {
    return "Her görsel en fazla 8 MB olabilir.";
  }
  if (file.size === 0) return "Boş görsel dosyası yüklenemez.";
  return null;
}

// Burada tek görseli ürün public kimliğine ait klasöre unsigned preset ile doğrudan yüklüyorum.
export async function uploadProductImage(
  item: ProductMediaDraftItem,
  productId: string,
  signal?: AbortSignal,
): Promise<CloudinaryProductAsset> {
  const validationError = validateProductImageFile(item.file);
  if (validationError) throw new Error(validationError);

  const { cloudName, uploadPreset } = getCloudinaryBrowserConfig();
  const folder = `products/${productId}`;
  const body = new FormData();
  body.set("file", item.file);
  body.set("upload_preset", uploadPreset);
  body.set("folder", folder);
  body.set("tags", `product-image,product-${productId}`);

  const response = await fetch(`https://api.cloudinary.com/v1_1/${encodeURIComponent(cloudName)}/image/upload`, {
    method: "POST",
    body,
    signal,
  });
  const payload = await response.json().catch(() => null) as CloudinaryUploadResponse | null;
  if (!response.ok) throw new Error("Görsel yüklenemedi. Lütfen tekrar deneyin.");

  const imageUrl = typeof payload?.secure_url === "string" ? payload.secure_url : "";
  const publicId = typeof payload?.public_id === "string" ? payload.public_id : "";
  if (!isTrustedCloudinaryProductAsset({ imageUrl, publicId }, productId, cloudName)) {
    throw new Error("Görsel yükleme yanıtı doğrulanamadı. Yöneticiyle iletişime geçin.");
  }

  return { clientKey: item.key, imageUrl, publicId };
}

// Burada yüklemeleri sıra bilgisini koruyarak aynı anda en fazla üç istekle çalıştırıyorum.
export async function uploadProductImages(
  items: ProductMediaDraftItem[],
  productId: string,
  signal?: AbortSignal,
): Promise<ProductImageUploadBatch> {
  const uploaded: Array<CloudinaryProductAsset | undefined> = new Array(items.length);
  const failed: ProductImageUploadBatch["failed"] = [];
  let cursor = 0;

  const worker = async () => {
    while (cursor < items.length) {
      const index = cursor;
      cursor += 1;
      const item = items[index];
      try {
        uploaded[index] = await uploadProductImage(item, productId, signal);
      } catch (error) {
        failed.push({
          clientKey: item.key,
          fileName: item.file.name,
          message: error instanceof Error ? error.message : "Görsel yüklenemedi.",
        });
      }
    }
  };

  await Promise.all(
    Array.from({ length: Math.min(PRODUCT_IMAGE_UPLOAD_CONCURRENCY, items.length) }, () => worker()),
  );
  return { uploaded: uploaded.filter((asset): asset is CloudinaryProductAsset => Boolean(asset)), failed };
}
