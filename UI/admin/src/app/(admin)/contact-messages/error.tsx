"use client";

// Burada liste hatasını hassas response içeriği göstermeden route düzeyinde tekrar denenebilir sunuyorum.
export default function ContactMessagesError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return <section role="alert" className="rounded-xl border border-danger/30 bg-surface p-6"><h1 className="text-xl font-semibold text-foreground">İletişim mesajları yüklenemedi</h1><p className="mt-2 max-w-2xl text-sm leading-6 text-muted">Oturum, yetki veya bağlantı kaynaklı bir sorun oluştu. Güvenli biçimde tekrar deneyebilirsiniz.</p><button type="button" onClick={reset} className="mt-4 min-h-10 rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">Tekrar dene</button></section>;
}
