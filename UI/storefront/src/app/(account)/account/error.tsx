"use client";

// Burada özel hesap verisi yüklenemediğinde güvenli tekrar deneme seçeneği sunuyorum.
export default function AccountError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <section className="border border-line bg-surface px-6 py-10 text-center" aria-labelledby="account-error-title">
      <h1 id="account-error-title" className="text-2xl font-black text-brand-950">Hesap bilgileri açılamadı</h1>
      <p className="mx-auto mt-3 max-w-lg text-sm leading-6 text-ink-muted">Bağlantı sırasında bir sorun oluştu. Bilgileriniz değiştirilmedi.</p>
      <button type="button" onClick={reset} className="focus-ring mt-6 min-h-11 bg-brand-950 px-5 text-sm font-bold text-white hover:bg-brand-700">Tekrar dene</button>
    </section>
  );
}
