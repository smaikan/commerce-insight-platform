import type { Metadata } from "next";

import { GuestReturnDetailView } from "@/modules/returns/components/guest-return-detail-view";

export const metadata: Metadata = { title: "Misafir Talep Detayı", robots: { index: false, follow: false, noarchive: true } };

export default async function GuestReturnDetailPage({ params }: { params: Promise<{ orderId: string; returnId: string }> }) {
  const value = await params;
  return <GuestReturnDetailView orderId={value.orderId} returnId={value.returnId} />;
}
