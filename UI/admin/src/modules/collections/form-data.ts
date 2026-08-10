import type { components } from "@/generated/api";
import {
  isTrustedCloudinaryAsset,
  type CloudinaryAsset,
} from "../../lib/cloudinary/browser-upload";
import type { CollectionActionState } from "./types";

type CollectionFormValue = components["schemas"]["CollectionRequest"];

// Burada koleksiyon formunu backend doğrulayıcılarıyla aynı uzunluk ve sıra sınırlarında okuyorum.
export function parseCollectionForm(formData: FormData): {
  ok: true;
  value: CollectionFormValue;
  imageMode: "keep" | "remove" | "replace";
  imageAsset?: CloudinaryAsset;
} | { ok: false; state: CollectionActionState } {
  const name = text(formData, "name");
  const url = text(formData, "url");
  const description = text(formData, "description");
  const imageModeValue = text(formData, "imageMode");
  const imageMode = imageModeValue === "remove" || imageModeValue === "replace" ? imageModeValue : "keep";
  const imageUrl = text(formData, "imageUrl");
  const imagePublicId = text(formData, "imagePublicId");
  const displayOrder = Number(text(formData, "displayOrder") || "0");
  const fieldErrors: Record<string, string[]> = {};

  if (!name) fieldErrors.name = ["Koleksiyon adı zorunludur."];
  else if (name.length > 150) fieldErrors.name = ["Koleksiyon adı en fazla 150 karakter olabilir."];
  if (url.length > 200) fieldErrors.url = ["Bağlantı en fazla 200 karakter olabilir."];
  if (description.length > 1000) fieldErrors.description = ["Açıklama en fazla 1.000 karakter olabilir."];
  if (!Number.isInteger(displayOrder) || displayOrder < 0) fieldErrors.displayOrder = ["Görüntüleme sırası sıfır veya daha büyük bir tam sayı olmalıdır."];

  const imageAsset: CloudinaryAsset | undefined = imageMode === "replace"
    ? { secureUrl: imageUrl, publicId: imagePublicId, resourceType: "image" }
    : undefined;
  if (imageUrl.length > 500) fieldErrors.imageUrl = ["Görsel adresi en fazla 500 karakter olabilir."];
  if (imageMode === "replace" && !isTrustedCollectionImageAsset(imageAsset)) {
    fieldErrors.imageUrl = ["Görsel kaynağı doğrulanamadı."];
  }
  if (Object.keys(fieldErrors).length > 0) {
    return { ok: false, state: { status: "error", message: "Form alanlarını kontrol edin.", fieldErrors } };
  }
  return {
    ok: true,
    value: { name, url: url || null, description: description || null, displayOrder, imageUrl: null },
    imageMode,
    imageAsset,
  };
}

// Burada görsel varlığının yapılandırılmış hesaptaki koleksiyon klasörüne ait olduğunu doğruluyorum.
export function isTrustedCollectionImageAsset(asset?: CloudinaryAsset, id?: string): boolean {
  if (!asset) return false;
  const folder = id ? `collections/${id}` : asset.publicId.split("/").slice(0, -1).join("/");
  return folder.startsWith("collections/")
    && isTrustedCloudinaryAsset(asset, { folder, allowedResourceTypes: ["image"] });
}

// Burada tekil form metnini boşlukları ayıklanmış biçimde alıyorum.
function text(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value.trim() : "";
}
