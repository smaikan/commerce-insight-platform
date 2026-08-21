"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

// Burada sayfa numarasına doğrudan klavyeyle veya git butonuyla atlamayı sağlayan kontrollü istemci formunu tanımlıyorum.
export function PageJumpForm({
  currentPage,
  totalPages,
  hrefTemplate,
}: {
  currentPage: number;
  totalPages: number;
  hrefTemplate: string;
}) {
  const [targetPage, setTargetPage] = useState("");
  const router = useRouter();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const pageNum = Number.parseInt(targetPage, 10);
    if (!Number.isFinite(pageNum) || pageNum < 1 || pageNum > totalPages) {
      return;
    }
    router.push(hrefTemplate.replace("__PAGE__", String(pageNum)));
  };

  return (
    <form onSubmit={handleSubmit} className="inline-flex items-center gap-1.5" aria-label="Sayfaya git">
      <label htmlFor="jump-page-input" className="sr-only">Sayfa Numarası</label>
      <input
        id="jump-page-input"
        type="number"
        min={1}
        max={totalPages}
        placeholder={String(currentPage)}
        value={targetPage}
        onChange={(e) => setTargetPage(e.target.value)}
        className="w-14 sm:w-16 h-8 sm:h-9 rounded-lg border border-line bg-surface px-1.5 sm:px-2 text-center text-xs sm:text-sm font-medium text-ink focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
        aria-label={`Sayfa numarası (1-${totalPages})`}
      />
      <button
        type="submit"
        disabled={!targetPage || Number(targetPage) < 1 || Number(targetPage) > totalPages}
        className="h-8 sm:h-9 px-2.5 sm:px-3 rounded-lg bg-brand-700 text-xs font-semibold text-white transition-colors hover:bg-brand-950 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
      >
        Git
      </button>
    </form>
  );
}
