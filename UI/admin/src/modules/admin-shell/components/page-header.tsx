import Link from "next/link";

export function PageHeader({
  title,
  description,
  actions,
  backHref,
  backLabel,
}: {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  backHref?: string;
  backLabel?: string;
}) {
  const resolvedBackLabel = backLabel ?? getBackLabel(backHref);

  return (
    <header className="mb-4 flex flex-col gap-3 border-l-4 border-primary pl-3 sm:flex-row sm:items-start sm:justify-between">
      <div className="min-w-0">
        {backHref ? (
          <Link href={backHref} className="mb-1 inline-flex min-h-8 items-center text-sm font-medium text-primary hover:text-primary-hover">
            ← {resolvedBackLabel}
          </Link>
        ) : null}
        <h1 className="text-xl font-semibold tracking-tight text-foreground">{title}</h1>
        {description ? (
          <p className="mt-1 max-w-3xl text-sm leading-5 text-muted">{description}</p>
        ) : null}
      </div>
      {actions ? <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div> : null}
    </header>
  );
}

// Burada detay ve form ekranlarının dönüş bağlantısını hedef kaynağa göre açık biçimde adlandırıyorum.
function getBackLabel(backHref: string | undefined): string {
  if (!backHref) return "Listeye dön";

  if (backHref === "/products") return "Ürünlere dön";
  if (backHref === "/orders") return "Siparişlere dön";
  if (backHref === "/customers") return "Müşterilere dön";
  if (backHref.startsWith("/contact-messages")) return "İletişim mesajlarına dön";
  if (backHref === "/collections") return "Koleksiyonlara dön";
  if (backHref === "/coupons") return "İndirimlere dön";
  if (backHref === "/managers") return "Yöneticilere dön";
  if (backHref === "/inventory/stock-movements") return "Stok işlemlerine dön";
  if (backHref === "/accounting") return "Ön muhasebeye dön";
  if (backHref === "/accounting/current-accounts") return "Cari hesaplara dön";
  if (backHref === "/accounting/purchase-invoices") return "Alış faturalarına dön";
  if (backHref === "/accounting/sales-orders") return "Muhasebe satışlarına dön";
  if (backHref === "/accounting/sales-invoices") return "Satış faturalarına dön";
  if (backHref === "/settings/shipping-methods") return "Kargo yöntemlerine dön";
  if (backHref === "/settings/tax-rates") return "Vergi oranlarına dön";
  if (backHref.startsWith("/settings/catalog/")) return "Katalog tanımlarına dön";

  return "Listeye dön";
}
