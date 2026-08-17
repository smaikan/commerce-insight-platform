import type { Metadata } from "next";
import { notFound } from "next/navigation";

import { ApiError } from "@/lib/api/problem";
import { getAccountReturn } from "@/modules/account/api";
import { withAccountSession } from "@/modules/account/session";
import { ReturnDetail } from "@/modules/returns/components/return-detail";

export const metadata: Metadata = { title: "Talep Detayı" };

// Burada kullanıcıya ait olmayan iade kimliğini ayrıntı sızdırmadan 404 davranışına dönüştürüyorum.
export default async function AccountReturnDetailPage({ params }: { params: Promise<{ returnId: string }> }) {
  const { returnId } = await params;
  let value;
  try {
    value = await withAccountSession(`/account/returns/${returnId}`, () => getAccountReturn(returnId));
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }
  return <ReturnDetail value={value} backHref="/account/returns" />;
}
