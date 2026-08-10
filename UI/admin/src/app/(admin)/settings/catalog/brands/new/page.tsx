import { redirect } from "next/navigation";

// Burada eski marka oluşturma bağlantısını bağımsız canonical forma yönlendiriyorum.
export default function LegacyNewBrandPage() {
  redirect("/brands/new");
}
