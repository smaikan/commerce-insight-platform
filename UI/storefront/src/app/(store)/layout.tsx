import { SiteFooter } from "@/components/storefront/site-footer";
import { SiteHeader } from "@/components/storefront/site-header";
import { HeaderSessionProvider } from "@/modules/auth/components/header-session";

// Burada public mağaza rotalarına ortak header, footer ve ana içeriğe geçiş bağlantısını route grubu sınırında ekliyorum.
export default function StoreLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <HeaderSessionProvider>
      <a className="skip-link" href="#main-content">Ana içeriğe geç</a>
      <SiteHeader />
      <div className="flex flex-1 flex-col">{children}</div>
      <SiteFooter />
    </HeaderSessionProvider>
  );
}
