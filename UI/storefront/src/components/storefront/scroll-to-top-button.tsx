"use client";

import { useEffect, useState } from "react";

// Burada sayfa yukarı kaydırıldığında gizlenen, aşağı kaydırıldığında ise yumuşak geçişle beliren lüks başa dön butonunu yönetiyorum.
export function ScrollToTopButton() {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    function handleScroll() {
      // 320px aşağı inildiğinde butonu göster, tepedeyken gizle
      setIsVisible(window.scrollY > 320);
    }

    // İlk yüklemede mevcut kaydırma konumunu kontrol et
    handleScroll();

    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, []);

  function scrollToTop() {
    window.scrollTo({
      top: 0,
      behavior: "smooth",
    });
  }

  return (
    <div
      className={`fixed right-4 bottom-4 sm:right-6 sm:bottom-6 z-30 transition-all duration-300 ease-out ${
        isVisible
          ? "opacity-100 translate-y-0 scale-100 pointer-events-auto"
          : "opacity-0 translate-y-4 scale-90 pointer-events-none"
      }`}
    >
      <button
        type="button"
        onClick={scrollToTop}
        aria-label="Sayfanın başına dön"
        className="group relative flex size-11 sm:size-12 items-center justify-center !rounded-full rounded-[9999px] bg-brand-950/85 text-white backdrop-blur-md border border-white/20 shadow-lg shadow-brand-950/25 transition-all duration-200 hover:bg-brand-950 hover:border-brand-500/50 hover:shadow-2xl hover:scale-110 active:scale-95 cursor-pointer outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2"
        style={{ borderRadius: "9999px" }}
      >
        <svg
          aria-hidden="true"
          viewBox="0 0 24 24"
          className="size-5 transition-transform duration-200 group-hover:-translate-y-0.5"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="m18 15-6-6-6 6" />
        </svg>

        {/* Masaüstü Hover Bilgilendirme Baloncuğu */}
        <span
          role="tooltip"
          className="pointer-events-none absolute right-full mr-2.5 hidden rounded-lg bg-brand-950/95 px-2.5 py-1 text-[11px] font-semibold tracking-wide text-white shadow-md backdrop-blur-sm opacity-0 -translate-x-1 transition-all duration-200 group-hover:translate-x-0 group-hover:opacity-100 sm:block whitespace-nowrap border border-white/10"
        >
          Başa Dön
        </span>
      </button>
    </div>
  );
}
