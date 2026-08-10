import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getCatalogItem } from "@/modules/settings/catalog-api";
import { catalogResourceConfigs, isCatalogResource } from "@/modules/settings/catalog-resource";
import type { CatalogItem } from "@/modules/settings/catalog-types";
import { CatalogForm } from "@/modules/settings/components/catalog-form";
import { CatalogTabs } from "@/modules/settings/components/catalog-tabs";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";

export const metadata: Metadata = { title: "Katalog tanımını düzenle" };

// Burada katalog kaydını kimliğiyle okuyup bulunamayan veya geçersiz kaynağı doğru 404 sınırına gönderiyorum.
export default async function EditCatalogItemPage({ params }: { params: Promise<{ resource: string; id: string }> }) {
  const { resource, id } = await params;
  if (!isCatalogResource(resource)) notFound();
  const session = await requireAdminPageSession(`/settings/catalog/${resource}/${encodeURIComponent(id)}`);
  let item: CatalogItem;
  try { item = await getCatalogItem(resource, id, session); } catch (error) { if (error instanceof ApiError && error.problem.status === 404) notFound(); throw error; }
  const config = catalogResourceConfigs[resource];
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title={`${config.singularTitle} düzenle`} description={item.name} backHref={`/settings/catalog/${resource}`} /><SettingsFrame activeHref="/settings/catalog/product-types"><CatalogTabs activeResource={resource} /><CatalogForm resource={resource} item={item} /></SettingsFrame></div>;
}
