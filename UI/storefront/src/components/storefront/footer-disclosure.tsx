"use client";

import { useState } from "react";

// Burada footer bölümlerini yalnız mobilde erişilebilir disclosure kontrolüne dönüştürüp masaüstünde sürekli açık tutuyorum.
export function FooterDisclosure({
  id,
  title,
  children,
}: {
  id: string;
  title: string;
  children: React.ReactNode;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const titleId = `${id}-title`;
  const panelId = `${id}-panel`;

  return (
    <section className="border-t border-footer-line sm:border-0" aria-labelledby={titleId}>
      <h2 id={titleId} className="text-sm font-bold tracking-[0.08em] uppercase">
        <button
          type="button"
          className="focus-ring flex min-h-12 w-full items-center justify-between text-left sm:hidden"
          aria-expanded={isOpen}
          aria-controls={panelId}
          onClick={() => setIsOpen((current) => !current)}
        >
          <span>{title}</span>
          <svg
            aria-hidden="true"
            viewBox="0 0 20 20"
            className={`size-4 transition-transform ${isOpen ? "rotate-180" : ""}`}
            fill="none"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinecap="round"
          >
            <path d="m6 8 4 4 4-4" />
          </svg>
        </button>
        <span className="hidden sm:block">{title}</span>
      </h2>
      <div id={panelId} className={`${isOpen ? "block" : "hidden"} pb-5 sm:block sm:pb-0`}>
        {children}
      </div>
    </section>
  );
}
