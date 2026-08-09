import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getTaxRate } from "@/modules/settings/api";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";
import { TaxRateForm } from "@/modules/settings/components/tax-rate-form";

export const metadata: Metadata = { title: "Vergi oranını düzenle" };

// Burada tek vergi oranını kimliğiyle okuyup bulunamayan kaynağı doğru 404 sınırına gönderiyorum.
export default async function EditTaxRatePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const session = await requireAdminPageSession(`/settings/tax-rates/${encodeURIComponent(id)}`);
  let taxRate;
  try { taxRate = await getTaxRate(id, session); } catch (error) { if (error instanceof ApiError && error.problem.status === 404) notFound(); throw error; }
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Vergi oranını düzenle" description={taxRate.name} backHref="/settings/tax-rates" /><SettingsFrame activeHref="/settings/tax-rates"><TaxRateForm taxRate={taxRate} /></SettingsFrame></div>;
}
