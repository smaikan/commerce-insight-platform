import type { SettingsActionState } from "@/modules/settings/types";

// Burada ayar formlarının ortak kontrol yüksekliği, sınırı ve odak görünümünü tek sınıfta tutuyorum.
export const settingsInputClass = "min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-focus/30";

// Burada ayar formlarındaki etiket, yardım metni ve alan hatasını tek erişilebilir düzende topluyorum.
export function SettingsField({ label, htmlFor, error, hint, className, children }: { label: string; htmlFor: string; error?: string; hint?: string; className?: string; children: React.ReactNode }) {
  return <div className={className}><label htmlFor={htmlFor} className="text-sm font-medium text-foreground">{label}</label><div className="mt-1.5">{children}</div>{hint && !error ? <p className="mt-1 text-xs text-muted">{hint}</p> : null}{error ? <p className="mt-1 text-xs font-medium text-danger">{error}</p> : null}</div>;
}

// Burada genel API hatasını formlarda aynı başlık ve takip kimliğiyle gösteriyorum.
export function SettingsActionError({ state, className = "" }: { state: SettingsActionState; className?: string }) {
  return <div role="alert" className={`rounded-xl border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger ${className}`}><p className="font-semibold">İşlem tamamlanamadı</p><p className="mt-1">{state.message}</p>{state.traceId ? <p className="mt-2 font-mono text-xs">Takip: {state.traceId}</p> : null}</div>;
}

// Burada backend alan adlarını büyük-küçük harf farkından bağımsız form alanlarıyla eşliyorum.
export function getSettingsFieldError(state: SettingsActionState, name: string): string | undefined {
  const key = Object.keys(state.fieldErrors ?? {}).find((candidate) => candidate.toLocaleLowerCase("tr-TR") === name.toLocaleLowerCase("tr-TR"));
  return key ? state.fieldErrors?.[key]?.[0] : undefined;
}
