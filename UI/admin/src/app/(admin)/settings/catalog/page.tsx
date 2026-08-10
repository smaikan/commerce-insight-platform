import { redirect } from "next/navigation";

// Burada katalog ayarları kökünü generic tanımların ilk sekmesi olan ürün türlerine yönlendiriyorum.
export default function CatalogSettingsPage() {
  redirect("/settings/catalog/product-types");
}
