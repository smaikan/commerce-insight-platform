import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getCustomers } from "@/modules/customers/api";
import { CustomerFilters } from "@/modules/customers/components/customer-filters";
import { CustomerPagination } from "@/modules/customers/components/customer-pagination";
import { CustomerTable } from "@/modules/customers/components/customer-table";
import { buildCustomerListHref, parseCustomerListQuery } from "@/modules/customers/query";

export const metadata: Metadata = { title: "Müşteriler" };

// Burada URL tabanlı belgelenmiş filtreleri okuyup yönetici müşteri listesini server-side hazırlıyorum.
export default async function CustomersPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const query = parseCustomerListQuery(await searchParams);
  const session = await requireAdminPageSession(buildCustomerListHref(query, query.pageNumber));
  const page = await getCustomers(query, session);

  return (
    <div className="w-full">
      <PageHeader
        title="Müşteriler"
        description="Müşterileri ad, e-posta ve hesap durumuna göre filtreleyin."
      />

      <section
        aria-label="Müşteri listesi"
        className="overflow-hidden rounded-xl border border-border bg-surface"
      >
        <CustomerFilters query={query} />
        <CustomerTable page={page} query={query} />
        <CustomerPagination page={page} query={query} />
      </section>
    </div>
  );
}
