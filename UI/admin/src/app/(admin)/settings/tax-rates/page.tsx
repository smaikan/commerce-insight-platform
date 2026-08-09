import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getTaxRates } from "@/modules/settings/api";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";
import { TaxRateList } from "@/modules/settings/components/tax-rate-list";
import { parseSettingsListQuery, settingsListHref } from "@/modules/settings/query";

export const metadata: Metadata = { title: "Vergi oranları" };

// Burada vergi oranlarını URL sayfalaması ve doğrulanmış Admin oturumuyla server-side getiriyorum.
export default async function TaxRatesPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const params = await searchParams;
  const query = parseSettingsListQuery(params);
  const session = await requireAdminPageSession(settingsListHref("/settings/tax-rates", query, query.pageNumber));
  const page = await getTaxRates(query, session);
  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader title="Vergi oranları" description="Ürünlerde kullanılan vergi oranlarını ve seçimlerdeki aktiflik durumlarını yönetin." actions={<Link href="/settings/tax-rates/new" className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Vergi oranı ekle</Link>} />
      <SettingsFrame activeHref="/settings/tax-rates">
        {params.created === "1" ? <SuccessMessage>Vergi oranı oluşturuldu.</SuccessMessage> : null}
        {params.updated === "1" ? <SuccessMessage>Vergi oranı güncellendi.</SuccessMessage> : null}
        <TaxRateList page={page} query={query} />
      </SettingsFrame>
    </div>
  );
}

// Burada başarılı liste dönüşünü ekran okuyucuya da duyuruyorum.
function SuccessMessage({ children }: { children: React.ReactNode }) {
  return <p role="status" className="mb-4 rounded-xl border border-success/25 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{children}</p>;
}
