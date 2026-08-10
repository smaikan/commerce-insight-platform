import {
  getCloudinaryBrowserConfig as getSharedCloudinaryBrowserConfig,
  uploadCloudinaryAsset,
  validateImageFile,
} from "../../lib/cloudinary/browser-upload";
import {
  isTrustedCloudinaryProductAsset,
  PRODUCT_IMAGE_UPLOAD_CONCURRENCY,
  type CloudinaryProductAsset,
  type ProductMediaDraftItem,
} from "./product-media";

export type ProductImageUploadBatch = {
  uploaded: CloudinaryProductAsset[];
  failed: Array<{ clientKey: string; fileName: string; message: string }>;
};

// Burada tarayıcıya açık ortak Cloudinary hesap ve görsel preset bilgilerini okuyorum.
export function getCloudinaryBrowserConfig(): { cloudName: string; uploadPreset: string } {
  return getSharedCloudinaryBrowserConfig();
}

// Burada ürün görselini ortak sekiz MB ve dosya türü kurallarıyla doğruluyorum.
export function validateProductImageFile(file: Pick<File, "type" | "size">): string | null {
  return validateImageFile(file);
}

// Burada tek görseli ürün public kimliğine ait klasöre unsigned preset ile doğrudan yüklüyorum.
export async function uploadProductImage(
  item: ProductMediaDraftItem,
  productId: string,
  signal?: AbortSignal,
): Promise<CloudinaryProductAsset> {
  const validationError = validateProductImageFile(item.file);
  if (validationError) throw new Error(validationError);

  const { cloudName } = getCloudinaryBrowserConfig();
  const asset = await uploadCloudinaryAsset({
    file: item.file,
    folder: `products/${productId}`,
    tags: ["product-image", `product-${productId}`],
    resourceType: "image",
  }, signal);
  const productAsset: CloudinaryProductAsset = {
    clientKey: item.key,
    imageUrl: asset.secureUrl,
    publicId: asset.publicId,
  };
  if (!isTrustedCloudinaryProductAsset(productAsset, productId, cloudName)) {
    throw new Error("Görsel yükleme yanıtı doğrulanamadı. Yöneticiyle iletişime geçin.");
  }
  return productAsset;
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

  // Burada her işçinin sıradaki dosyayı alıp sonucunu özgün dizin konumunda saklamasını sağlıyorum.
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
