"use client";

// Burada ayar verisi yüklenemediğinde güvenli yeniden deneme ve anlaşılır hata durumu sunuyorum.
export default function SettingsError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return <div className="mx-auto w-full max-w-screen-2xl rounded-xl border border-danger/30 bg-surface px-5 py-10 text-center"><h1 className="text-lg font-semibold text-foreground">Ayarlar yüklenemedi</h1><p className="mx-auto mt-2 max-w-lg text-sm text-muted">API geçici olarak yanıt vermiyor olabilir. Mevcut veriler değiştirilmedi.</p><button type="button" onClick={reset} className="mt-4 min-h-10 rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Tekrar dene</button></div>;
}
