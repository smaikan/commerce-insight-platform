"use client";

import { useEffect, useRef, useState } from "react";

const HEADER_HIDE_START = 96;
const SCROLL_DIRECTION_THRESHOLD = 6;

type HeaderVisibilityInput = {
  previousScrollY: number;
  currentScrollY: number;
  hidden: boolean;
};

// Burada küçük scroll titreşimlerini yok sayıp aşağı harekette gizlenen, yukarı harekette açılan header kararını saf biçimde üretiyorum.
export function nextHeaderHiddenState({ previousScrollY, currentScrollY, hidden }: HeaderVisibilityInput): boolean {
  if (currentScrollY <= 0) return false;

  const movement = currentScrollY - previousScrollY;
  if (Math.abs(movement) < SCROLL_DIRECTION_THRESHOLD) return hidden;
  if (movement < 0) return false;
  return currentScrollY > HEADER_HIDE_START;
}

// Burada ana navigasyonu scroll yönüne göre kaydırırken ilan şeridini bu sticky davranışın dışında bırakıyorum.
export function ScrollAwareHeader({ children }: { children: React.ReactNode }) {
  const [hidden, setHidden] = useState(false);
  const previousScrollY = useRef(0);
  const frame = useRef<number | null>(null);

  useEffect(() => {
    previousScrollY.current = Math.max(window.scrollY, 0);

    // Burada yoğun scroll olaylarını tek animation frame içinde birleştirerek header hareketini akıcı tutuyorum.
    function handleScroll() {
      if (frame.current !== null) return;

      frame.current = window.requestAnimationFrame(() => {
        frame.current = null;
        const currentScrollY = Math.max(window.scrollY, 0);
        const priorScrollY = previousScrollY.current;
        previousScrollY.current = currentScrollY;
        setHidden((currentHidden) => nextHeaderHiddenState({
          previousScrollY: priorScrollY,
          currentScrollY,
          hidden: currentHidden,
        }));
      });
    }

    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => {
      window.removeEventListener("scroll", handleScroll);
      if (frame.current !== null) window.cancelAnimationFrame(frame.current);
    };
  }, []);

  return (
    <header
      data-scroll-state={hidden ? "hidden" : "visible"}
      onFocusCapture={() => setHidden(false)}
      className={`relative sticky top-0 z-40 border-b border-line/80 bg-surface transition-transform duration-300 ease-out motion-reduce:transition-none ${hidden ? "-translate-y-full" : "translate-y-0"}`}
    >
      {children}
    </header>
  );
}
