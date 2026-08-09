import { redirect } from "next/navigation";

// Burada katalog ayarları kökünü varsayılan marka sekmesine yönlendiriyorum.
export default function CatalogSettingsPage() {
  redirect("/settings/catalog/brands");
}
