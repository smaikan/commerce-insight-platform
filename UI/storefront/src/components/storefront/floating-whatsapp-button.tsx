"use client";

// Burada mağazanın sol alt köşesinde dolaşan, canlı nabız göstergeli ve hover tepkili lüks WhatsApp destek butonunu sunuyorum.
export function FloatingWhatsappButton({ href }: { href: string }) {
  return (
    <div className="fixed left-4 bottom-4 sm:left-6 sm:bottom-6 z-30">
      <a
        href={href}
        target="_blank"
        rel="noreferrer"
        aria-label="WhatsApp Canlı Destek ile iletişime geç"
        className="group relative flex size-12 sm:size-13 items-center justify-center !rounded-full rounded-[9999px] bg-[#25D366] text-white shadow-xl shadow-emerald-950/20 transition-all duration-300 hover:shadow-2xl hover:shadow-emerald-500/40 hover:scale-110 active:scale-95 cursor-pointer outline-none focus-visible:ring-2 focus-visible:ring-emerald-400 focus-visible:ring-offset-2"
        style={{ borderRadius: "9999px" }}
      >
        {/* Canlı Çevrimiçi Nabız Göstergesi */}
        <span className="absolute top-0 right-0 flex size-3.5" aria-hidden="true">
          <span className="absolute inline-flex size-full animate-ping !rounded-full rounded-[9999px] bg-emerald-300 opacity-75 motion-reduce:animate-none" />
          <span className="relative inline-flex size-3.5 !rounded-full rounded-[9999px] bg-emerald-400 border-2 border-white shadow-xs" />
        </span>

        {/* WhatsApp İkonu */}
        <svg
          aria-hidden="true"
          viewBox="0 0 24 24"
          className="size-6 sm:size-7 transition-transform duration-300 group-hover:scale-105"
          fill="currentColor"
        >
          <path d="M12.04 2c-5.46 0-9.91 4.45-9.91 9.91 0 1.75.46 3.45 1.32 4.95L2.05 22l5.25-1.38c1.45.79 3.08 1.21 4.74 1.21 5.46 0 9.91-4.45 9.91-9.91 0-2.65-1.03-5.14-2.9-7.01A9.82 9.82 0 0 0 12.04 2m.01 1.67c2.2 0 4.26.86 5.82 2.42a8.23 8.23 0 0 1 2.41 5.83c0 4.54-3.7 8.24-8.24 8.24-1.48 0-2.93-.4-4.2-1.15l-.3-.18-3.12.82.83-3.04-.2-.31a8.19 8.19 0 0 1-1.26-4.38c0-4.54 3.7-8.24 8.24-8.24M8.53 7.33c-.16 0-.42.06-.64.3-.22.25-.85.83-.85 2.02 0 1.2.87 2.35 1 2.51.12.16 1.7 2.6 4.12 3.65.58.25 1.03.4 1.38.51.58.18 1.11.16 1.53.1.47-.07 1.44-.59 1.64-1.16.2-.57.2-1.06.14-1.16-.06-.1-.22-.16-.47-.28-.25-.13-1.44-.71-1.66-.8-.22-.08-.39-.13-.55.12-.16.25-.64.8-.78.96-.14.17-.29.19-.54.06-.25-.12-1.05-.39-2-1.23-.74-.66-1.24-1.48-1.39-1.73-.14-.25-.02-.38.11-.5.11-.11.25-.29.37-.43.13-.15.17-.25.25-.42.09-.17.04-.31-.02-.44-.06-.12-.55-1.33-.76-1.82-.2-.48-.41-.41-.56-.42l-.48-.01Z" />
        </svg>

        {/* Masaüstü Hover Bilgilendirme Kartı */}
        <span
          role="tooltip"
          className="pointer-events-none absolute left-full ml-3 hidden rounded-xl bg-brand-950/95 px-3 py-1.5 text-xs font-semibold tracking-wide text-white shadow-xl backdrop-blur-sm opacity-0 -translate-x-1 transition-all duration-200 group-hover:translate-x-0 group-hover:opacity-100 sm:flex items-center gap-2 whitespace-nowrap border border-white/15"
        >
          <span className="size-2 !rounded-full rounded-[9999px] bg-[#25D366] animate-pulse" />
          <span>WhatsApp Destek</span>
        </span>
      </a>
    </div>
  );
}
