import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getOptionalAdminSession } from "@/lib/auth/session";
import { clearRejectedSessionAction } from "@/modules/auth/actions";

export const metadata: Metadata = { title: "Erişim Reddedildi" };

// Burada geçerli Admin oturumunu panele döndürüp diğer rollere hiçbir panel içeriği göstermeyen 403 yüzeyi sunuyorum.
export default async function AccessDeniedPage() {
  if (await getOptionalAdminSession()) redirect("/dashboard");

  return (
    <main className="flex min-h-dvh items-center justify-center bg-page px-4 py-10">
      <section aria-labelledby="access-denied-title" className="w-full max-w-md rounded-xl border border-border bg-surface-strong p-6">
        <p className="text-sm font-semibold text-danger">Erişim reddedildi</p>
        <h1 id="access-denied-title" className="mt-2 text-2xl font-semibold text-foreground">Admin rolü gerekli</h1>
        <p className="mt-3 text-sm leading-6 text-muted">Bu yönetim paneline yalnızca aktif Admin rolündeki hesaplar erişebilir.</p>
        <form action={clearRejectedSessionAction} className="mt-6">
          <button type="submit" className="inline-flex min-h-11 w-full items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">
            Oturumu temizle ve girişe dön
          </button>
        </form>
      </section>
    </main>
  );
}
