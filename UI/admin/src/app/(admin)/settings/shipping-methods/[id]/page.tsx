import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getShippingMethod } from "@/modules/settings/api";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";
import { ShippingMethodForm } from "@/modules/settings/components/shipping-method-form";

export const metadata: Metadata = { title: "Kargo yöntemini düzenle" };

// Burada tek kargo yöntemini kimliğiyle okuyup bulunamayan kaynağı doğru 404 sınırına gönderiyorum.
export default async function EditShippingMethodPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const session = await requireAdminPageSession(`/settings/shipping-methods/${encodeURIComponent(id)}`);
  let method;
  try { method = await getShippingMethod(id, session); } catch (error) { if (error instanceof ApiError && error.problem.status === 404) notFound(); throw error; }
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Kargo yöntemini düzenle" description={method.name} backHref="/settings/shipping-methods" /><SettingsFrame activeHref="/settings/shipping-methods"><ShippingMethodForm method={method} /></SettingsFrame></div>;
}
