import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";
import { ShippingMethodForm } from "@/modules/settings/components/shipping-method-form";

export const metadata: Metadata = { title: "Kargo yöntemi ekle" };

// Burada yeni kargo yöntemi formunu doğrulanmış Admin sınırında sunuyorum.
export default async function NewShippingMethodPage() {
  await requireAdminPageSession("/settings/shipping-methods/new");
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Kargo yöntemi ekle" description="Müşterilerin checkout sırasında seçebileceği yeni bir teslimat yöntemi tanımlayın." backHref="/settings/shipping-methods" /><SettingsFrame activeHref="/settings/shipping-methods"><ShippingMethodForm /></SettingsFrame></div>;
}
