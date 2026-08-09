import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { catalogResourceConfigs, isCatalogResource } from "@/modules/settings/catalog-resource";
import { CatalogForm } from "@/modules/settings/components/catalog-form";
import { CatalogTabs } from "@/modules/settings/components/catalog-tabs";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";

export const metadata: Metadata = { title: "Katalog tanımı ekle" };

// Burada seçilen kaynak için yalnızca desteklenen alanları içeren yeni kayıt formunu sunuyorum.
export default async function NewCatalogItemPage({ params }: { params: Promise<{ resource: string }> }) {
  const { resource } = await params;
  if (!isCatalogResource(resource)) notFound();
  const config = catalogResourceConfigs[resource];
  await requireAdminPageSession(`/settings/catalog/${resource}/new`);
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title={`${config.singularTitle} ekle`} description={config.description} backHref={`/settings/catalog/${resource}`} /><SettingsFrame activeHref="/settings/catalog/brands"><CatalogTabs activeResource={resource} /><CatalogForm resource={resource} /></SettingsFrame></div>;
}
