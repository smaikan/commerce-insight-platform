import type { Metadata } from "next";

import { SiteHeader } from "@/components/storefront/site-header";
import { AuthShell } from "@/modules/auth/components/auth-shell";
import { RegisterForm } from "@/modules/auth/components/register-form";

export const metadata: Metadata = {
  title: "Hesap oluştur",
  description: "Alışveriş deneyiminizi hızlandırmak için güvenli bir müşteri hesabı oluşturun.",
};

// Burada kayıt formunu ortak mağaza header'ının altında, kişisel veriyi URL veya metadata'ya taşımayan sunucu kabuğunda sunuyorum.
export default function RegisterPage() {
  return (
    <>
      <SiteHeader />
      <AuthShell
        eyebrow="Yeni bir başlangıç"
        title="Alışverişin sana ayak uydursun."
        description="Hesabını oluştur; sipariş ve adres işlemlerini tek, güvenli bir alanda yönet."
      >
        <RegisterForm />
      </AuthShell>
    </>
  );
}
