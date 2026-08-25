import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { retryAfterSeconds } from "@/lib/api/problem";
import { siteConfig } from "@/lib/site-config";
import { getOptionalAdminSession } from "@/lib/auth/session";
import { safeReturnTo } from "@/lib/auth/policy";
import { LoginForm } from "@/modules/auth/components/login-form";
import { getPublicStoreSettings } from "@/modules/settings/api";

export const metadata: Metadata = { title: "Yönetici Girişi" };

// Burada doğrulanmış Admin oturumunu panele yönlendirip diğer tüm durumlarda güvenli login ekranını oluşturuyorum.
export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const session = await getOptionalAdminSession();
  if (session) redirect("/dashboard");

  const params = await searchParams;
  const returnTo = safeReturnTo(singleParam(params.returnTo));
  const notice = loginNotice(singleParam(params.reason), singleParam(params.retryAfter));
  // Burada API erişilemezse girişi engellemeden nötr yapılandırma adına geri dönüyorum.
  const store = await getPublicStoreSettings().catch(() => ({
    displayName: siteConfig.name,
  }));

  return (
    <main className="flex min-h-dvh items-center justify-center bg-page px-4 py-10 sm:px-6">
      <section aria-labelledby="login-title" className="w-full max-w-md rounded-xl border border-border bg-surface-strong p-5 sm:p-7">
        <div className="border-b border-border pb-5">
          {/* Burada giriş ekranındaki admin kimliğini de gerçek mağaza adıyla tutarlı gösteriyorum. */}
          <p className="truncate text-sm font-semibold text-primary">{store.displayName}</p>
          <h1 id="login-title" className="mt-2 text-2xl font-semibold tracking-tight text-foreground">Yönetici girişi</h1>
          <p className="mt-2 text-sm leading-6 text-muted">Yönetim paneline yalnızca aktif Admin rolündeki hesaplar erişebilir.</p>
        </div>
        <LoginForm returnTo={returnTo} notice={notice} />
      </section>
    </main>
  );
}

// Burada query parametrelerindeki çoklu veya bilinmeyen değerleri tek güvenli metne indiriyorum.
function singleParam(value: string | string[] | undefined): string | undefined {
  return typeof value === "string" ? value : undefined;
}

// Burada route nedenlerini hassas auth ayrıntısı ifşa etmeyen kullanıcı mesajlarına çeviriyorum.
function loginNotice(reason: string | undefined, retryAfter: string | undefined): string | undefined {
  if (reason === "logged_out") return "Oturumunuz güvenli biçimde kapatıldı.";
  if (reason === "forbidden") return "Bu panel yalnızca aktif yönetici hesaplarına açıktır.";
  if (reason === "session_expired") return "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";
  if (reason === "session_required") return "Devam etmek için yönetici hesabınızla giriş yapın.";
  if (reason === "verification_failed") return "Oturum şu anda doğrulanamıyor. Lütfen tekrar deneyin.";
  if (reason === "refresh_rate_limited") {
    const waitSeconds = retryAfterSeconds(retryAfter);
    return waitSeconds
      ? `Oturumunuz korunuyor. Yenileme trafiği kısa süreli sınırlandı; ${waitSeconds} saniye sonra tekrar deneyin.`
      : "Oturumunuz korunuyor. Yenileme trafiği kısa süreli sınırlandı; lütfen biraz sonra tekrar deneyin.";
  }
  return undefined;
}
