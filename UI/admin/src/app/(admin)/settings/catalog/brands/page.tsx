import { redirect } from "next/navigation";

// Burada eski marka ayarları bağlantısını bağımsız canonical marka listesine yönlendiriyorum.
export default function LegacyBrandsPage() {
  redirect("/brands");
}
