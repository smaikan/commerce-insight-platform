"use client";

import { useState } from "react";

export type BankCardProgram = {
  id: string;
  name: string;
  banks: string;
  rates: {
    count: number;
    rate: number;
    isFree?: boolean;
  }[];
};

// Burada iyzico altyapısında desteklenen 8 ana banka kartı ailesini ve taksit oranlarını tanımlıyorum.
export const BANK_CARD_PROGRAMS: BankCardProgram[] = [
  {
    id: "world",
    name: "World",
    banks: "Yapı Kredi, VakıfBank, Albaraka, Anadolubank",
    rates: [
      { count: 1, rate: 0 },
      { count: 2, rate: 0, isFree: true },
      { count: 3, rate: 0, isFree: true },
      { count: 6, rate: 0.055 },
      { count: 9, rate: 0.095 },
      { count: 12, rate: 0.145 },
    ],
  },
  {
    id: "bonus",
    name: "Bonus",
    banks: "Garanti BBVA, DenizBank, TEB, Şekerbank, Fibabanka",
    rates: [
      { count: 1, rate: 0 },
      { count: 2, rate: 0, isFree: true },
      { count: 3, rate: 0, isFree: true },
      { count: 6, rate: 0.055 },
      { count: 9, rate: 0.095 },
      { count: 12, rate: 0.145 },
    ],
  },
  {
    id: "maximum",
    name: "Maximum",
    banks: "Türkiye İş Bankası",
    rates: [
      { count: 1, rate: 0 },
      { count: 2, rate: 0, isFree: true },
      { count: 3, rate: 0, isFree: true },
      { count: 6, rate: 0.055 },
      { count: 9, rate: 0.095 },
      { count: 12, rate: 0.145 },
    ],
  },
  {
    id: "cardfinans",
    name: "CardFinans",
    banks: "QNB Finansbank",
    rates: [
      { count: 1, rate: 0 },
      { count: 2, rate: 0, isFree: true },
      { count: 3, rate: 0, isFree: true },
      { count: 6, rate: 0.055 },
      { count: 9, rate: 0.095 },
      { count: 12, rate: 0.145 },
    ],
  },
  {
    id: "axess",
    name: "Axess",
    banks: "Akbank",
    rates: [
      { count: 1, rate: 0 },
      { count: 2, rate: 0, isFree: true },
      { count: 3, rate: 0, isFree: true },
      { count: 6, rate: 0.055 },
      { count: 9, rate: 0.095 },
      { count: 12, rate: 0.145 },
    ],
  },
  {
    id: "paraf",
    name: "Paraf",
    banks: "Halkbank",
    rates: [
      { count: 1, rate: 0 },
      { count: 2, rate: 0, isFree: true },
      { count: 3, rate: 0, isFree: true },
      { count: 6, rate: 0.055 },
      { count: 9, rate: 0.095 },
      { count: 12, rate: 0.145 },
    ],
  },
  {
    id: "bankkart",
    name: "Bankkart Combo",
    banks: "Ziraat Bankası",
    rates: [
      { count: 1, rate: 0 },
      { count: 2, rate: 0, isFree: true },
      { count: 3, rate: 0, isFree: true },
      { count: 6, rate: 0.055 },
      { count: 9, rate: 0.095 },
      { count: 12, rate: 0.145 },
    ],
  },
  {
    id: "advantage",
    name: "Advantage",
    banks: "HSBC",
    rates: [
      { count: 1, rate: 0 },
      { count: 2, rate: 0, isFree: true },
      { count: 3, rate: 0, isFree: true },
      { count: 6, rate: 0.055 },
      { count: 9, rate: 0.095 },
      { count: 12, rate: 0.145 },
    ],
  },
];

type ProductInstallmentTableProps = {
  price: number;
  currency?: string;
};

// Burada iyzico standartlarına uygun banka bazlı ayrıştırılmış ve tam mobil uyumlu taksit tablosunu sunuyorum.
export function ProductInstallmentTable({ price, currency = "TRY" }: ProductInstallmentTableProps) {
  const [selectedBankId, setSelectedBankId] = useState<string>("world");
  const activeProgram =
    BANK_CARD_PROGRAMS.find((program) => program.id === selectedBankId) || BANK_CARD_PROGRAMS[0];

  const formatter = new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
  });

  return (
    <details className="group border-t border-line py-5">
      <summary className="focus-ring flex cursor-pointer list-none items-center justify-between gap-3 text-sm font-bold text-ink hover:text-brand-700 transition-colors [&::-webkit-details-marker]:hidden">
        <div className="flex items-center gap-2.5">
          <span
            aria-hidden="true"
            className="flex size-7 shrink-0 items-center justify-center rounded-md bg-brand-50 text-brand-700"
          >
            <svg viewBox="0 0 24 24" fill="none" className="size-4" stroke="currentColor" strokeWidth="1.8">
              <rect x="2" y="5" width="20" height="14" rx="2" />
              <line x1="2" y1="10" x2="22" y2="10" />
            </svg>
          </span>
          <span>Taksit seçenekleri</span>
        </div>
        <svg
          aria-hidden="true"
          viewBox="0 0 24 24"
          fill="none"
          className="size-4 shrink-0 text-ink-muted transition-transform duration-200 group-open:rotate-180 motion-reduce:transition-none"
          stroke="currentColor"
          strokeWidth="2"
        >
          <path d="m6 9 6 6 6-6" />
        </svg>
      </summary>

      <div className="mt-4 space-y-3.5">
        {/* Banka Kartı Seçim Sekmeleri (iyzico banka sekmeleri) */}
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 scrollbar-none" role="tablist" aria-label="Banka kartı programları">
          {BANK_CARD_PROGRAMS.map((program) => {
            const isSelected = program.id === activeProgram.id;
            return (
              <button
                key={program.id}
                type="button"
                role="tab"
                aria-selected={isSelected}
                onClick={() => setSelectedBankId(program.id)}
                className={`shrink-0 cursor-pointer rounded-lg px-2.5 py-1.5 text-xs font-semibold transition-all ${
                  isSelected
                    ? "bg-ink text-white shadow-xs"
                    : "bg-surface-subtle text-ink-muted hover:bg-line/70 hover:text-ink"
                }`}
              >
                {program.name}
              </button>
            );
          })}
        </div>

        {/* Seçili Banka Kart Ailesi Açıklaması */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1 rounded-lg bg-surface-subtle/60 px-3 py-2 text-xs text-ink-muted border border-line/50">
          <span className="leading-snug">
            <strong className="text-ink font-semibold">{activeProgram.name}</strong>: {activeProgram.banks}
          </span>
          <span className="text-[11px] font-semibold text-brand-700 shrink-0">iyzico Güvencesi</span>
        </div>

        {/* Taksit Tablosu - Mobilde taşmayan sabit oranlı yapı */}
        <div className="overflow-hidden rounded-xl border border-line bg-surface w-full max-w-full">
          <table className="w-full table-fixed border-collapse text-left text-xs">
            <caption className="sr-only">{activeProgram.name} kartı için yaklaşık taksit tutarları</caption>
            <colgroup>
              <col className="w-[38%]" />
              <col className="w-[31%]" />
              <col className="w-[31%]" />
            </colgroup>
            <thead className="bg-surface-subtle border-b border-line text-ink-muted">
              <tr>
                <th scope="col" className="px-2.5 py-2.5 font-semibold sm:px-3.5">Taksit</th>
                <th scope="col" className="px-2 py-2.5 text-right font-semibold sm:px-3.5">Aylık Tutar</th>
                <th scope="col" className="px-2 py-2.5 text-right font-semibold sm:px-3.5">Toplam Tutar</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-line">
              {activeProgram.rates.map(({ count, rate, isFree }) => {
                const total = price * (1 + rate);
                const monthly = total / count;
                return (
                  <tr
                    key={count}
                    className={`transition-colors hover:bg-surface-subtle/40 ${
                      isFree ? "bg-emerald-50/25" : ""
                    }`}
                  >
                    <th scope="row" className="px-2.5 py-2 font-medium text-ink sm:px-3.5 sm:py-2.5 align-middle">
                      <div className="flex flex-col items-start gap-0.5">
                        <span className="font-semibold leading-tight">{count === 1 ? "Tek çekim" : `${count} taksit`}</span>
                        {isFree ? (
                          <span className="inline-flex rounded bg-emerald-100/90 px-1 py-0.5 text-[9px] font-bold text-emerald-800 uppercase tracking-tight leading-none">
                            Vade Farksız
                          </span>
                        ) : null}
                      </div>
                    </th>
                    <td className="px-2 py-2 text-right font-semibold tabular-nums text-ink sm:px-3.5 sm:py-2.5 text-[11px] sm:text-xs align-middle">
                      {formatter.format(monthly)}
                    </td>
                    <td className="px-2 py-2 text-right tabular-nums text-ink-muted font-medium sm:px-3.5 sm:py-2.5 text-[11px] sm:text-xs align-middle">
                      {formatter.format(total)}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {/* iyzico Güvenlik ve Vade Farkı Bilgilendirmesi */}
        <div className="flex items-start gap-2 rounded-lg bg-surface-subtle/60 p-2.5 text-[11px] leading-relaxed text-ink-muted border border-line/60">
          <svg className="size-4 shrink-0 text-brand-700 mt-0.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
            <path d="M7 11V7a5 5 0 0 1 10 0v4" />
          </svg>
          <p>
            Kartınıza uygun taksit sayısı, uygulanabilecek vade farkı ve kesin tahsilat tutarı <strong>iyzico 256-Bit SSL</strong> korumalı ödeme sayfasında kart bilgileri girildiğinde gösterilir.
          </p>
        </div>
      </div>
    </details>
  );
}
