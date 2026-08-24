export const MAX_PRODUCT_IMAGES = 10;
export const MAX_PRODUCT_IMAGE_BYTES = 8 * 1024 * 1024;
export const PRODUCT_IMAGE_UPLOAD_CONCURRENCY = 3;
export const ACCEPTED_PRODUCT_IMAGE_TYPES = ["image/jpeg", "image/png", "image/webp"] as const;

export type ProductMediaDraftItem = {
  key: string;
  file: File;
};

export type ProductMediaDraft = {
  localMedia: ProductMediaDraftItem[];
  mainKey: string | null;
  orderedKeys: string[];
};

export type CloudinaryProductAsset = {
  clientKey: string;
  imageUrl: string;
  publicId: string;
};

export type ProductMediaCommitInput = {
  productId: string;
  mainExistingImageId?: string;
  existingImages: Array<{
    id: string;
    displayOrder: number;
  }>;
  newImages: Array<CloudinaryProductAsset & {
    displayOrder: number;
    isMain: boolean;
  }>;
};

export type ProductMediaCommitResult = {
  status: "success" | "partial" | "error";
  productId: string;
  message?: string;
  traceId?: string;
  committedClientKeys: string[];
  updatedExistingImageIds: string[];
};

// Burada sürüklenen veya klavye kontrolleriyle taşınan medya anahtarını hedef konuma yerleştiriyorum.
export function moveMediaKey(keys: string[], sourceKey: string, targetKey: string): string[] {
  const sourceIndex = keys.indexOf(sourceKey);
  const targetIndex = keys.indexOf(targetKey);
  if (sourceIndex < 0 || targetIndex < 0 || sourceIndex === targetIndex) return keys;

  const next = [...keys];
  const [moved] = next.splice(sourceIndex, 1);
  next.splice(targetIndex, 0, moved);
  return next;
}

// Burada API'ye iletilecek Cloudinary kaydının beklenen ürün klasörüne ve hesaba ait olduğunu doğruluyorum.
export function isTrustedCloudinaryProductAsset(
  asset: Pick<CloudinaryProductAsset, "imageUrl" | "publicId">,
  productId: string,
  cloudName: string,
): boolean {
  const folderPrefix = `products/${productId}/`;
  if (!asset.publicId.startsWith(folderPrefix)) return false;

  try {
    const url = new URL(asset.imageUrl);
    const decodedPath = decodeURIComponent(url.pathname);
    return url.protocol === "https:"
      && url.hostname === "res.cloudinary.com"
      && decodedPath.startsWith(`/${cloudName}/image/upload/`)
      && decodedPath.includes(`/${folderPrefix}`);
  } catch {
    return false;
  }
}
