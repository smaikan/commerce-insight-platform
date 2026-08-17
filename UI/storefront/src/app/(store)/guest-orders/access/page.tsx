import type { Metadata } from "next";

import { GuestAccessView } from "@/modules/returns/components/guest-access-view";

export const metadata: Metadata = { title: "Misafir Sipariş Erişimi", robots: { index: false, follow: false, noarchive: true } };

export default function GuestAccessPage() {
  return <main id="main-content" className="page-shell flex flex-1 items-center justify-center py-12 sm:py-20"><div className="w-full max-w-lg border border-line bg-surface p-6 shadow-panel sm:p-9"><GuestAccessView /></div></main>;
}
