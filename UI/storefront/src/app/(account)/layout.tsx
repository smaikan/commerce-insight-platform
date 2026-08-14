import type { Metadata } from "next";
import { redirect } from "next/navigation";

import { SiteFooter } from "@/components/storefront/site-footer";
import { SiteHeader } from "@/components/storefront/site-header";
import { hasAuthSessionCookie } from "@/lib/auth/cookies";
import { AccountSidebar } from "@/modules/account/components/account-sidebar";
import { HeaderSessionProvider } from "@/modules/auth/components/header-session";

export const metadata: Metadata = {
  title: "Hesabım",
  robots: { index: false, follow: false, noarchive: true },
};

// Burada kişisel veri entegrasyonundan önce dahi hesap kabuğunu oturum çerezi varlığıyla sınırlandırıp noindex olarak sunuyorum.
export default async function AccountLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  if (!await hasAuthSessionCookie()) redirect("/login?returnTo=/account");

  return (
    <HeaderSessionProvider>
      <a className="skip-link" href="#main-content">Ana içeriğe geç</a>
      <SiteHeader />
      <main id="main-content" className="flex-1 bg-background py-8 sm:py-12">
        <div className="page-shell grid gap-8 lg:grid-cols-[14rem_minmax(0,1fr)] lg:gap-12">
          <aside className="min-w-0">
            <p className="mb-3 px-3 text-[0.6875rem] font-bold tracking-[0.14em] text-ink-muted uppercase">Hesabım</p>
            <AccountSidebar />
          </aside>
          <div className="min-w-0">{children}</div>
        </div>
      </main>
      <SiteFooter />
    </HeaderSessionProvider>
  );
}
