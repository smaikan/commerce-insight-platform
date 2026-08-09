import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getCatalogItems } from "@/modules/settings/catalog-api";
import { catalogResourceConfigs, isCatalogResource } from "@/modules/settings/catalog-resource";
import { CatalogList } from "@/modules/settings/components/catalog-list";
import { CatalogTabs } from "@/modules/settings/components/catalog-tabs";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";
import { parseSettingsListQuery, settingsListHref } from "@/modules/settings/query";

export const metadata: Metadata = { title: "Katalog tanımları" };

// Burada seçilen katalog kaynağını doğrulayıp belgeli sayfalama ile server-side getiriyorum.
export default async function CatalogResourcePage({ params, searchParams }: { params: Promise<{ resource: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const [{ resource }, queryParams] = await Promise.all([params, searchParams]);
  if (!isCatalogResource(resource)) notFound();
  const config = catalogResourceConfigs[resource];
  const query = parseSettingsListQuery(queryParams);
  const basePath = `/settings/catalog/${resource}`;
  const session = await requireAdminPageSession(settingsListHref(basePath, query, query.pageNumber));
  const page = await getCatalogItems(resource, query, session);
  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader title="Katalog tanımları" description="Ürünlerde kullanılan marka, ürün türü ve etiket seçeneklerini yönetin." actions={<Link href={`${basePath}/new`} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">{config.singularTitle} ekle</Link>} />
      <SettingsFrame activeHref="/settings/catalog/brands">
        <CatalogTabs activeResource={resource} />
        <section className="mb-4"><h2 className="text-lg font-semibold text-foreground">{config.title}</h2><p className="mt-1 text-sm text-muted">{config.description}</p></section>
        {queryParams.created === "1" ? <SuccessMessage>{config.singularTitle} oluşturuldu.</SuccessMessage> : null}
        {queryParams.updated === "1" ? <SuccessMessage>{config.singularTitle} güncellendi.</SuccessMessage> : null}
        <CatalogList resource={resource} page={page} query={query} />
      </SettingsFrame>
    </div>
  );
}

// Burada başarılı katalog işlemini ekran okuyucuya da duyuruyorum.
function SuccessMessage({ children }: { children: React.ReactNode }) {
  return <p role="status" className="mb-4 rounded-xl border border-success/25 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{children}</p>;
}
