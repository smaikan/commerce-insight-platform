"use client";

import { useState } from "react";

function formatCardNumber(cardNumber: string): string {
  return cardNumber.replace(/\D/g, "").replace(/(.{4})/g, "$1 ").trim();
}

// Burada sandbox kartını ödeme alanına dönüştürmeden yalnız erişilebilir, seçilebilir ve kopyalanabilir test bilgisi olarak sunuyorum.
export function SandboxPaymentNotice({ cardNumber }: { cardNumber: string }) {
  const [copyMessage, setCopyMessage] = useState("");
  const normalizedCardNumber = cardNumber.replace(/\D/g, "");
  const formattedCardNumber = formatCardNumber(normalizedCardNumber);

  async function copyCardNumber() {
    try {
      if (!navigator.clipboard?.writeText) throw new Error("Clipboard API unavailable");
      await navigator.clipboard.writeText(normalizedCardNumber);
      setCopyMessage("Test kartı numarası kopyalandı.");
    } catch {
      setCopyMessage("Otomatik kopyalama kullanılamıyor. Numarayı seçerek kopyalayabilirsiniz.");
    }
  }

  return (
    <aside className="rounded-xl border border-brand-200 bg-brand-50/60 px-4 py-3.5 text-left" aria-labelledby="sandbox-payment-title">
      <div className="flex items-start gap-3">
        <span className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-lg bg-surface text-brand-700" aria-hidden="true">
          <svg viewBox="0 0 24 24" className="size-4" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <rect x="3" y="5" width="18" height="14" rx="2" />
            <path d="M3 10h18" />
          </svg>
        </span>
        <div className="min-w-0 flex-1">
          <p id="sandbox-payment-title" className="text-sm font-bold text-brand-950">Test ödeme ortamı</p>
          <p className="mt-1 text-xs leading-5 text-ink-muted">
            Gerçek kart kullanmayın. Bu bilgi yalnız sandbox ödeme denemeleri için gösterilir.
          </p>
        </div>
      </div>

      <details className="group mt-3 border-t border-brand-200 pt-2">
        <summary className="focus-ring flex min-h-11 cursor-pointer list-none items-center justify-between gap-3 text-sm font-bold text-brand-700 hover:text-brand-950 [&::-webkit-details-marker]:hidden">
          <span>Test kartını göster</span>
          <svg aria-hidden="true" viewBox="0 0 24 24" className="size-4 shrink-0 transition-transform duration-200 group-open:rotate-180 motion-reduce:transition-none" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="m6 9 6 6 6-6" />
          </svg>
        </summary>

        <div className="pb-1 pt-2">
          <p className="text-xs font-semibold text-ink-muted">Kart numarası</p>
          <div className="mt-2 flex flex-col gap-2">
            <code className="flex min-h-11 w-full select-all items-center justify-center overflow-x-auto whitespace-nowrap rounded-lg border border-line bg-surface px-3 py-2 font-mono text-sm font-bold tracking-[0.08em] text-ink" aria-label={`Test kartı numarası ${formattedCardNumber}`}>
              {formattedCardNumber}
            </code>
            <button
              type="button"
              onClick={() => void copyCardNumber()}
              className="focus-ring inline-flex min-h-11 w-full cursor-pointer items-center justify-center rounded-lg border border-brand-700 px-4 text-xs font-bold text-brand-700 hover:bg-surface"
            >
              Numarayı kopyala
            </button>
          </div>
          <p className="mt-2 text-xs leading-5 text-ink-muted">
            Kart alanları iyzico’nun güvenli sayfasında açılır; mağaza kart bilgisi toplamaz veya saklamaz.
          </p>
          <div className="mt-2 rounded-lg border border-brand-200 bg-surface px-3 py-2.5 text-xs leading-5 text-ink-muted">
            <p className="font-bold text-ink">Diğer kart alanları</p>
            <p className="mt-1">
              Kart sahibi adına herhangi bir test adı, son kullanma tarihine gelecekteki bir tarihi <span className="font-semibold text-ink">AA/YY</span> formatında, CVV alanına ise herhangi bir <span className="font-semibold text-ink">3 haneli sayı</span> yazabilirsiniz.
            </p>
          </div>
          <p className="mt-1 min-h-5 text-xs font-semibold text-brand-700" role="status" aria-live="polite">
            {copyMessage}
          </p>
        </div>
      </details>
    </aside>
  );
}
