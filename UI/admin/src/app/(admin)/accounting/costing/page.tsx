import type { Metadata } from "next";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getCostingVariant, getOpeningBalanceLayer, getVariantCostHistory, searchCostingVariants } from "@/modules/accounting/costing/api";
import { CostingWorkspace } from "@/modules/accounting/costing/components/costing-workspace";
import { parseCostingQuery } from "@/modules/accounting/costing/query";

export const metadata: Metadata = { title: "FIFO Maliyet Yönetimi" };

// Burada seçim ve maliyet okumalarını aynı yönetici oturumuyla, birbirinden bağımsız API çağrıları olarak yürütüyorum.
export default async function CostingPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const query = parseCostingQuery(await searchParams);
  const session = await requireAdminPageSession("/accounting/costing");
  let data;
  try {
    const result = await searchCostingVariants(query.search, session);
    const selected = query.productVariantId ? await getCostingVariant(query.productVariantId, result.items, session) : null;
    const [layer, history] = selected ? await Promise.all([getOpeningBalanceLayer(selected.id, session), getVariantCostHistory(selected.id, session)]) : [null, []];
    data = { result, selected, layer, history };
  } catch (error) {
    if (error instanceof ApiError) return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="FIFO Maliyet Yönetimi" description="Açılış stok maliyetlerini ve maliyet geçmişini yönetin." backHref="/accounting" /><AccountingLoadProblem problem={error.problem} retryHref="/accounting/costing" /></div>;
    throw error;
  }
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="FIFO Maliyet Yönetimi" description="Açılış stok maliyetlerini optimistic concurrency ile düzeltin ve varyant maliyet zincirini denetleyin." backHref="/accounting" /><CostingWorkspace query={query} options={data.result.items} selected={data.selected} layer={data.layer} history={data.history} truncated={data.result.truncated} /></div>;
}
