import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getShippingMethods } from "@/modules/settings/api";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";
import { ShippingMethodList } from "@/modules/settings/components/shipping-method-list";
import { parseSettingsListQuery, settingsListHref } from "@/modules/settings/query";

export const metadata: Metadata = { title: "Kargo yöntemleri" };

// Burada kargo yöntemlerini URL sayfalaması ve doğrulanmış Admin oturumuyla server-side getiriyorum.
export default async function ShippingMethodsPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const params = await searchParams;
  const query = parseSettingsListQuery(params);
  const session = await requireAdminPageSession(settingsListHref("/settings/shipping-methods", query, query.pageNumber));
  const page = await getShippingMethods(query, session);
  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader title="Kargo yöntemleri" description="Checkout'ta sunulan teslimat seçeneklerini, sabit ücretleri ve görüntülenme sırasını yönetin." actions={<Link href="/settings/shipping-methods/new" className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Kargo yöntemi ekle</Link>} />
      <SettingsFrame activeHref="/settings/shipping-methods">
        {params.created === "1" ? <SuccessMessage>Kargo yöntemi oluşturuldu.</SuccessMessage> : null}
        {params.updated === "1" ? <SuccessMessage>Kargo yöntemi güncellendi.</SuccessMessage> : null}
        <ShippingMethodList page={page} query={query} />
      </SettingsFrame>
    </div>
  );
}

// Burada başarılı liste dönüşünü ekran okuyucuya da duyuruyorum.
function SuccessMessage({ children }: { children: React.ReactNode }) {
  return <p role="status" className="mb-4 rounded-xl border border-success/25 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{children}</p>;
}
