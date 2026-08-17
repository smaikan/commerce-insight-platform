import { uploadCloudinaryAsset, type CloudinaryAsset } from "../../../lib/cloudinary/browser-upload";
import {
  STORE_SETTINGS_MEDIA_SLOTS,
  type StoreSettingsMediaSlot,
} from "./media-slots";

// Burada StoreSettings görselini unsigned preset ile doğrudan Cloudinary'ye ekliyorum.
export async function replaceStoreSettingsMedia(
  slot: StoreSettingsMediaSlot,
  file: File,
  signal?: AbortSignal,
): Promise<CloudinaryAsset> {
  const folder = STORE_SETTINGS_MEDIA_SLOTS[slot].folder;
  return uploadCloudinaryAsset({
    file,
    folder,
    tags: ["store-settings", folder.split("/").at(-1) || "image"],
  }, signal);
}
