import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getAdminBannerSections } from "@/modules/banners/api";
import { BannerManager } from "@/modules/banners/components/banner-manager";

export const metadata: Metadata = { title: "Bannerlar" };

// Burada altı bağımsız banner bölümünü paralel okuyup bölüm bazlı sonuçlarla yönetim ekranını kuruyorum.
export default async function BannersPage() {
  const session = await requireAdminPageSession("/banners");
  const sections = await getAdminBannerSections(session);

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title="Bannerlar"
        description="Ana vitrini ve beş bağımsız alt banner bölümünü görsel veya video kayıtlarıyla yönetin."
      />
      <BannerManager initialSections={sections} />
    </div>
  );
}
