import type { Metadata } from "next";

import { getAccountReturns } from "@/modules/account/api";
import { withAccountSession } from "@/modules/account/session";
import { ReturnsList } from "@/modules/returns/components/returns-list";

export const metadata: Metadata = { title: "İade ve Değişim" };

// Burada hesap sahibinin talep sayfasını güvenli sayfa numarası ve owner-scoped veriyle oluşturuyorum.
export default async function AccountReturnsPage({ searchParams }: { searchParams: Promise<{ page?: string }> }) {
  const parsed = Number((await searchParams).page);
  const page = Number.isInteger(parsed) && parsed > 0 ? Math.min(parsed, 10_000) : 1;
  const returns = await withAccountSession("/account/returns", () => getAccountReturns(page, 10));
  return <ReturnsList returns={returns} page={page} />;
}
