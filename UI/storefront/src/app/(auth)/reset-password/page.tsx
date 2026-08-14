import type { Metadata } from "next";

import { AuthShell } from "@/modules/auth/components/auth-shell";
import { ResetPasswordForm } from "@/modules/auth/components/reset-password-form";

export const metadata: Metadata = {
  title: "Yeni parola belirle",
  description: "Tek kullanımlık bağlantınızla müşteri hesabınız için yeni bir parola belirleyin.",
};

// Burada URL fragmentını yalnız istemci yaprağının okuyabildiği güvenli parola yenileme ekranını oluşturuyorum.
export default function ResetPasswordPage() {
  return (
    <AuthShell
      eyebrow="Güvenli parola yenileme"
      title="Yeni parolanı belirle."
      description="Yeni parolanı iki kez gir. İşlem tamamlandığında eski oturumların kapatılacak ve yeni parolanla giriş yapacaksın."
    >
      <ResetPasswordForm />
    </AuthShell>
  );
}
