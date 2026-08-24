"use client";

import { useState } from "react";

// Burada mağazanın VIP bülten ve yeni sezon fırsat duyurusu alanını sunuyorum.
export function NewsletterSection() {
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState<"idle" | "success">("idle");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !email.includes("@")) return;
    setStatus("success");
    setEmail("");
  };

  return (
    <section aria-labelledby="newsletter-heading" className="home-shell my-10 sm:my-16">
      <div className="relative overflow-hidden rounded-2xl border border-line bg-gradient-to-br from-surface to-surface-subtle px-6 py-12 text-center sm:px-12 sm:py-16 shadow-panel">
        <div className="max-w-xl mx-auto">
          <span className="text-xs font-bold uppercase tracking-[0.2em] text-brand-700">
            ELEVEN AYRICALIKLAR KULÜBÜ
          </span>
          <h2 id="newsletter-heading" className="mt-3 text-2xl font-bold tracking-tight text-ink sm:text-4xl">
            İlk Alışverişinizde %15 İndirim
          </h2>
          <p className="mt-4 text-sm sm:text-base leading-relaxed text-ink-muted">
            Yeni sezon koleksiyonları, sınırlı sayıda üretilen imza parçalar ve üyelere özel indirim kodlarından ilk siz haberdar olun.
          </p>

          {status === "success" ? (
            <div className="mt-8 rounded-xl bg-success/10 border border-success/30 p-4 text-success font-semibold text-sm">
              ✨ Teşekkürler! ELEVEN Kulübü bültenine başarıyla abone oldunuz.
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="mt-8 flex flex-col sm:flex-row gap-3 max-w-md mx-auto">
              <label htmlFor="newsletter-email" className="sr-only">
                E-posta adresiniz
              </label>
              <input
                id="newsletter-email"
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="E-posta adresinizi girin..."
                className="focus-ring flex-1 rounded-xl border border-line bg-surface px-4 py-3.5 text-sm text-ink placeholder:text-ink-muted/70 shadow-xs"
              />
              <button
                type="submit"
                className="focus-ring cursor-pointer rounded-xl bg-brand-950 px-6 py-3.5 text-sm font-bold text-white shadow-xs transition-all hover:bg-brand-700 shrink-0"
              >
                Katılın
              </button>
            </form>
          )}

          <p className="mt-3 text-[0.6875rem] text-ink-muted/80">
            Abone olarak Gizlilik Politikası ve Kişisel Verilerin Korunması koşullarını kabul etmiş olursunuz.
          </p>
        </div>
      </div>
    </section>
  );
}
