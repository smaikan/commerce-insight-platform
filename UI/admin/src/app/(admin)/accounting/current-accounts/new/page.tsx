import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { CurrentAccountForm } from "@/modules/accounting/current-accounts/components/current-account-form";

export const metadata: Metadata = { title: "Cari Hesap Oluştur" };

export default async function NewCurrentAccountPage() {
  await requireAdminPageSession("/accounting/current-accounts/new");
  return (
    <div className="mx-auto w-full max-w-6xl">
      <PageHeader title="Cari hesap oluştur" description="Müşteri, tedarikçi veya iki role sahip muhasebe master kaydı oluşturun." backHref="/accounting/current-accounts" />
      <CurrentAccountForm />
    </div>
  );
}
