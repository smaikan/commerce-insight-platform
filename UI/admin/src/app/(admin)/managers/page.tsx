import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getManagers } from "@/modules/managers/api";
import { ManagerList } from "@/modules/managers/components/manager-list";
import { managerHref, parseManagerQuery } from "@/modules/managers/query";

export const metadata: Metadata = { title: "Yöneticiler" };

// Burada yalnız Admin rolü filtrelenmiş yönetici listesini URL durumu ve doğrulanmış oturumla getiriyorum.
export default async function ManagersPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const params = await searchParams;
  const query = parseManagerQuery(params);
  const session = await requireAdminPageSession(managerHref(query));
  const page = await getManagers(query, session);

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title="Yöneticiler"
        description="Yönetim paneline erişimi olan Admin hesaplarını görüntüleyin."
        actions={(
          <Link
            href="/managers/new"
            className="inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
          >
            Yönetici ekle
          </Link>
        )}
      />
      {params.created === "1" ? (
        <p role="status" className="mb-4 rounded-xl border border-success/25 bg-success/10 px-4 py-3 text-sm font-semibold text-success">
          Yönetici oluşturuldu.
        </p>
      ) : null}
      <ManagerList page={page} query={query} />
    </div>
  );
}
