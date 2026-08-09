import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";
import { TaxRateForm } from "@/modules/settings/components/tax-rate-form";

export const metadata: Metadata = { title: "Vergi oranı ekle" };

// Burada yeni vergi oranı formunu doğrulanmış Admin sınırında sunuyorum.
export default async function NewTaxRatePage() {
  await requireAdminPageSession("/settings/tax-rates/new");
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Vergi oranı ekle" description="Ürünlerde kullanılabilecek yeni bir vergi tanımı oluşturun." backHref="/settings/tax-rates" /><SettingsFrame activeHref="/settings/tax-rates"><TaxRateForm /></SettingsFrame></div>;
}
