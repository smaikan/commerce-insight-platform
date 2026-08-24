"use client";

export default function AccountingError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div role="alert" className="rounded-xl border border-danger/30 bg-red-50 p-5 text-red-950">
      <h1 className="font-semibold">Ön muhasebe yüklenemedi</h1>
      <p className="mt-2 text-sm">Veriler alınırken beklenmeyen bir sorun oluştu.</p>
      <button type="button" onClick={reset} className="mt-4 min-h-10 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white">Tekrar dene</button>
    </div>
  );
}
