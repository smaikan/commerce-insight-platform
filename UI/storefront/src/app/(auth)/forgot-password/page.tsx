import type { Metadata } from "next";

import { AuthShell } from "@/modules/auth/components/auth-shell";
import { ForgotPasswordForm } from "@/modules/auth/components/forgot-password-form";

export const metadata: Metadata = {
  title: "Parolamı unuttum",
  description: "Müşteri hesabınız için güvenli parola sıfırlama bağlantısı isteyin.",
};

// Burada parola bağlantısı isteğini kişisel veriyi URL veya metadata alanına taşımayan auth kabuğunda sunuyorum.
export default function ForgotPasswordPage() {
  return (
    <AuthShell
      eyebrow="Hesap kurtarma"
      title="Hesabına yeniden eriş."
      description="Hesabında kullandığın e-posta adresini gir. Adres sistemde kayıtlıysa sana güvenli bir sıfırlama bağlantısı göndereceğiz."
    >
      <ForgotPasswordForm />
    </AuthShell>
  );
}
