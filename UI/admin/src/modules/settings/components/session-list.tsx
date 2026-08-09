import { formatSettingsDate } from "@/modules/settings/presentation";
import { RevokeAllSessionsDialog, RevokeSessionButton } from "@/modules/settings/components/session-actions";
import type { UserSession } from "@/modules/settings/types";

// Burada aktif oturumları cihaz, IP ve yaşam süresi bilgileriyle taranabilir bir güvenlik listesinde gösteriyorum.
export function SessionList({ sessions }: { sessions: UserSession[] }) {
  return (
    <div className="space-y-4">
      <section className="overflow-hidden rounded-xl border border-border bg-surface" aria-labelledby="active-sessions-title">
        <header className="flex flex-col gap-3 border-b border-border bg-surface-subtle/60 px-4 py-3.5 sm:flex-row sm:items-center sm:justify-between sm:px-5">
          <div><h2 id="active-sessions-title" className="text-base font-semibold text-foreground">Aktif oturumlar</h2><p className="mt-1 text-sm text-muted">Hesabınıza giriş yapılmış cihazları ve oturum sürelerini inceleyin.</p></div>
          <span className="text-xs font-semibold text-muted">{sessions.length} aktif oturum</span>
        </header>
        {sessions.length ? (
          <ul className="divide-y divide-border">
            {sessions.map((session) => {
              const deviceName = session.deviceName?.trim() || "Bilinmeyen cihaz";
              return (
                <li key={session.id} className="grid gap-4 px-4 py-4 sm:px-5 lg:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)_auto] lg:items-center">
                  <div className="flex min-w-0 items-start gap-3">
                    <span className="flex size-9 shrink-0 items-center justify-center rounded-lg border border-border bg-surface-subtle text-muted" aria-hidden="true"><DeviceIcon /></span>
                    <div className="min-w-0"><p className="truncate text-sm font-semibold text-foreground">{deviceName}</p><p className="mt-0.5 text-xs text-muted">IP: {session.createdByIp?.trim() || "Bilgi yok"}</p><p className="mt-1 font-mono text-[11px] text-muted">{session.id}</p></div>
                  </div>
                  <dl className="grid grid-cols-2 gap-3 text-xs lg:grid-cols-1">
                    <div><dt className="text-muted">Başlangıç</dt><dd className="mt-0.5 font-medium text-foreground">{formatSettingsDate(session.createdAt)}</dd></div>
                    <div><dt className="text-muted">Sona erme</dt><dd className="mt-0.5 font-medium text-foreground">{formatSettingsDate(session.expiresAt)}</dd></div>
                  </dl>
                  <RevokeSessionButton sessionId={session.id} deviceName={deviceName} />
                </li>
              );
            })}
          </ul>
        ) : <div className="px-5 py-12 text-center"><p className="font-semibold text-foreground">Aktif oturum bulunamadı</p><p className="mt-1 text-sm text-muted">Hesabınızla ilişkilendirilmiş aktif refresh oturumu görünmüyor.</p></div>}
      </section>
      <p className="rounded-xl border border-border bg-surface-subtle/60 px-4 py-3 text-xs leading-5 text-muted">Tek bir cihaz oturumunu sonlandırdığınızda o cihazın yeni erişim üretmesi engellenir. Kısa süreli mevcut erişimi bittiğinde yeniden giriş yapması gerekir.</p>
      <section className="flex flex-col gap-4 rounded-xl border border-danger/25 bg-surface p-4 sm:flex-row sm:items-center sm:justify-between sm:p-5">
        <div><h2 className="text-sm font-semibold text-foreground">Tüm cihazlardan çıkış</h2><p className="mt-1 max-w-2xl text-sm leading-5 text-muted">Şüpheli bir giriş fark ettiyseniz bütün access ve refresh oturumlarını geçersiz kılın.</p></div>
        <RevokeAllSessionsDialog sessionCount={sessions.length} />
      </section>
    </div>
  );
}

// Burada cihaz oturumunu ortak ikon setine yeni bağımlılık eklemeden tanımlıyorum.
function DeviceIcon() {
  return <svg viewBox="0 0 20 20" className="size-5 fill-none stroke-current" strokeWidth="1.7"><rect x="3" y="4" width="14" height="10" rx="1.5" /><path d="M7 17h6M10 14v3" strokeLinecap="round" /></svg>;
}
