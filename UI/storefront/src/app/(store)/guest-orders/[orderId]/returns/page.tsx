import type { Metadata } from "next";

import { GuestReturnsView } from "@/modules/returns/components/guest-returns-view";

export const metadata: Metadata = { title: "Misafir İade ve Değişim", robots: { index: false, follow: false, noarchive: true } };

export default async function GuestReturnsPage({ params }: { params: Promise<{ orderId: string }> }) {
  return <GuestReturnsView orderId={(await params).orderId} />;
}
