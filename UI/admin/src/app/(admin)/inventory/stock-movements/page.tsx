import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getStockBalance, getStockMovements } from "@/modules/inventory/api";
import { StockBalancePanel } from "@/modules/inventory/components/stock-balance-panel";
import { StockMovementFilters } from "@/modules/inventory/components/stock-movement-filters";
import { StockMovementPagination } from "@/modules/inventory/components/stock-movement-pagination";
import { StockMovementTable } from "@/modules/inventory/components/stock-movement-table";
import { parseStockMovementListQuery } from "@/modules/inventory/query";

export const metadata: Metadata = { title: "Stok İşlemleri" };

// Burada stok defterini ve istenirse seçili varyant mutabakatını doğrulanmış Admin oturumuyla birlikte getiriyorum.
export default async function StockMovementsPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const [session, params] = await Promise.all([requireAdminPageSession("/inventory/stock-movements"), searchParams]);
  const query = parseStockMovementListQuery(params);
  const [page, balanceResult] = await Promise.all([
    getStockMovements(query, session),
    query.balanceVariantId ? getStockBalance(query.balanceVariantId, session) : Promise.resolve(undefined),
  ]);
  const createdCount = Number(params.created);

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader title="Stok İşlemleri" description="Stok hareketlerini izleyin, filtreleyin ve kayıtlı stokla mutabakatını kontrol edin." actions={<Link href="/inventory/stock-movements/new" className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">Hareket oluştur</Link>} />
      {Number.isInteger(createdCount) && createdCount > 0 ? <p role="status" className="mb-4 rounded-xl border border-success/25 bg-success/10 px-4 py-3 text-sm font-semibold text-success">{createdCount} stok hareketi atomik olarak kaydedildi.</p> : null}
      <div className="grid min-w-0 gap-5 2xl:grid-cols-[minmax(0,1fr)_20rem]">
        <section aria-labelledby="stock-ledger-title" className="min-w-0 overflow-hidden rounded-xl border border-border bg-surface">
          <div className="border-b border-border bg-surface-subtle px-4 py-3 sm:px-5"><h2 id="stock-ledger-title" className="text-base font-semibold text-foreground">Stok hareket defteri</h2><p className="mt-1 text-sm text-muted">Her kayıt, oluştuğu stok değeriyle birlikte kalıcı denetim geçmişinde tutulur.</p></div>
          <StockMovementFilters query={query} />
          <StockMovementTable page={page} query={query} />
          <StockMovementPagination page={page} query={query} />
        </section>
        <StockBalancePanel balance={balanceResult} query={query} />
      </div>
    </div>
  );
}
