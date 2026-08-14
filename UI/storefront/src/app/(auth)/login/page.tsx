import type { Metadata } from "next";

import { SiteHeader } from "@/components/storefront/site-header";
import { safeReturnTo } from "@/lib/auth/policy";
import { LoginForm } from "@/modules/auth/components/login-form";
import { AuthShell } from "@/modules/auth/components/auth-shell";

export const metadata: Metadata = {
  title: "Giriş yap",
  description: "Hesabınıza güvenli biçimde giriş yapın.",
};

type LoginPageProps = {
  searchParams: Promise<{ registered?: string; autoLogin?: string; loggedOut?: string; returnTo?: string }>;
};

// Burada yalnızca güvenli dönüş hedefini forma taşıyıp kayıt sonrası bildirimi aynı auth kabuğunda gösteriyorum.
export default async function LoginPage({ searchParams }: LoginPageProps) {
  const query = await searchParams;

  // Burada yalnız login ekranına ortak responsive Storefront navbar'ını ekliyor, kayıt ekranının mevcut kompozisyonunu değiştirmiyorum.
  return (
    <>
      <SiteHeader />
      <AuthShell
        eyebrow="Tekrar hoş geldin"
        title="Kaldığın yerden devam et."
        description="Siparişlerini, adreslerini ve hesap ayarlarını güvenli oturumunla yönet."
      >
        <LoginForm
          returnTo={safeReturnTo(query.returnTo)}
          registered={query.registered === "1"}
          autoLoginFailed={query.autoLogin === "failed"}
          loggedOut={query.loggedOut === "1"}
        />
      </AuthShell>
    </>
  );
}
