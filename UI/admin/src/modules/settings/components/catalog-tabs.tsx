import Link from "next/link";
import { catalogResourceConfigs, type CatalogResource } from "@/modules/settings/catalog-resource";

// Burada katalog tanımlarını derin sidebar oluşturmadan üç erişilebilir sekmede ayırıyorum.
export function CatalogTabs({ activeResource }: { activeResource: CatalogResource }) {
  return (
    <nav aria-label="Katalog tanımı türleri" className="mb-4 flex gap-1 overflow-x-auto rounded-xl border border-border bg-surface p-1">
      {(Object.keys(catalogResourceConfigs) as CatalogResource[]).map((resource) => {
        const config = catalogResourceConfigs[resource];
        const active = resource === activeResource;
        return <Link key={resource} href={`/settings/catalog/${resource}`} aria-current={active ? "page" : undefined} className={`inline-flex min-h-10 shrink-0 cursor-pointer items-center rounded-lg px-4 text-sm font-semibold transition-colors ${active ? "bg-primary-soft text-primary-hover" : "text-muted hover:bg-surface-subtle hover:text-foreground"}`}>{config.title}</Link>;
      })}
    </nav>
  );
}
