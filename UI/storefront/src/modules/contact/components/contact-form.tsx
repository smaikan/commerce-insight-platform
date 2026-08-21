"use client";

import { useState } from "react";

export function ContactForm() {
  const [formData, setFormData] = useState({
    name: "",
    email: "",
    phone: "",
    subject: "order",
    orderNumber: "",
    message: "",
  });

  const [status, setStatus] = useState<"idle" | "submitting" | "success">("idle");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setStatus("submitting");

    // Form gönderimini simüle et ve kullanıcıya başarılı bildirimi göster
    setTimeout(() => {
      setStatus("success");
      setFormData({
        name: "",
        email: "",
        phone: "",
        subject: "order",
        orderNumber: "",
        message: "",
      });
    }, 600);
  };

  if (status === "success") {
    return (
      <div className="rounded-2xl border border-brand-200 bg-brand-50/50 p-8 text-center sm:p-10">
        <div className="mx-auto flex size-14 items-center justify-center rounded-full bg-brand-700 text-white shadow-sm">
          <svg className="size-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <h3 className="mt-4 text-xl font-bold text-brand-950">Mesajınız Bize Ulaştı</h3>
        <p className="mt-2 text-sm leading-relaxed text-ink-muted">
          Bizimle iletişime geçtiğiniz için teşekkür ederiz. Müşteri deneyimi ekibimiz mesajınızı inceleyip en kısa sürede sizinle iletişime geçecektir.
        </p>
        <button
          type="button"
          onClick={() => setStatus("idle")}
          className="focus-ring mt-6 inline-flex h-11 items-center justify-center rounded-xl bg-brand-950 px-6 text-sm font-semibold text-white transition-all hover:bg-brand-800"
        >
          Yeni Mesaj Gönder
        </button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5 rounded-2xl border border-line bg-surface p-6 shadow-sm sm:p-8">
      <div className="grid gap-5 sm:grid-cols-2">
        <div>
          <label htmlFor="contact-name" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">
            Adınız Soyadınız <span className="text-brand-600">*</span>
          </label>
          <input
            id="contact-name"
            type="text"
            required
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            placeholder="Örn. Zeynep Yılmaz"
            className="focus-ring mt-2 h-11 w-full rounded-xl border border-line bg-surface-subtle/30 px-3.5 text-sm text-ink transition-colors placeholder:text-ink-muted/60 hover:border-line-strong focus:border-brand-600 focus:bg-surface"
          />
        </div>

        <div>
          <label htmlFor="contact-email" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">
            E-posta Adresiniz <span className="text-brand-600">*</span>
          </label>
          <input
            id="contact-email"
            type="email"
            required
            value={formData.email}
            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            placeholder="ornek@alanadi.com"
            className="focus-ring mt-2 h-11 w-full rounded-xl border border-line bg-surface-subtle/30 px-3.5 text-sm text-ink transition-colors placeholder:text-ink-muted/60 hover:border-line-strong focus:border-brand-600 focus:bg-surface"
          />
        </div>
      </div>

      <div className="grid gap-5 sm:grid-cols-2">
        <div>
          <label htmlFor="contact-phone" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">
            Telefon Numarası
          </label>
          <input
            id="contact-phone"
            type="tel"
            value={formData.phone}
            onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
            placeholder="05XX XXX XX XX"
            className="focus-ring mt-2 h-11 w-full rounded-xl border border-line bg-surface-subtle/30 px-3.5 text-sm text-ink transition-colors placeholder:text-ink-muted/60 hover:border-line-strong focus:border-brand-600 focus:bg-surface"
          />
        </div>

        <div>
          <label htmlFor="contact-order" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">
            Sipariş Numarası <span className="text-xs font-normal lowercase text-ink-muted">(varsa)</span>
          </label>
          <input
            id="contact-order"
            type="text"
            value={formData.orderNumber}
            onChange={(e) => setFormData({ ...formData, orderNumber: e.target.value })}
            placeholder="Örn. ELV-2026-XXXX"
            className="focus-ring mt-2 h-11 w-full rounded-xl border border-line bg-surface-subtle/30 px-3.5 text-sm text-ink transition-colors placeholder:text-ink-muted/60 hover:border-line-strong focus:border-brand-600 focus:bg-surface"
          />
        </div>
      </div>

      <div>
        <label htmlFor="contact-subject" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">
          Konu <span className="text-brand-600">*</span>
        </label>
        <select
          id="contact-subject"
          required
          value={formData.subject}
          onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
          className="focus-ring mt-2 h-11 w-full rounded-xl border border-line bg-surface-subtle/30 px-3.5 text-sm text-ink transition-colors hover:border-line-strong focus:border-brand-600 focus:bg-surface"
        >
          <option value="order">Sipariş Takibi ve Durumu</option>
          <option value="product">Ürün Bilgisi ve Stok Danışmanlığı</option>
          <option value="return">İade, Değişim ve İptal Talebi</option>
          <option value="corporate">Kurumsal İş Birliği & Toptan Satış</option>
          <option value="feedback">Öneri, Görüş veya Şikayet</option>
          <option value="other">Diğer Konular</option>
        </select>
      </div>

      <div>
        <label htmlFor="contact-message" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">
          Mesajınız <span className="text-brand-600">*</span>
        </label>
        <textarea
          id="contact-message"
          required
          rows={5}
          value={formData.message}
          onChange={(e) => setFormData({ ...formData, message: e.target.value })}
          placeholder="Talebinizi, sorunuzu veya görüşlerinizi ayrıntılı olarak yazabilirsiniz..."
          className="focus-ring mt-2 w-full rounded-xl border border-line bg-surface-subtle/30 p-3.5 text-sm text-ink transition-colors placeholder:text-ink-muted/60 hover:border-line-strong focus:border-brand-600 focus:bg-surface"
        />
      </div>

      <button
        type="submit"
        disabled={status === "submitting"}
        className="focus-ring inline-flex h-12 w-full items-center justify-center rounded-xl bg-brand-950 px-6 text-sm font-semibold text-white transition-all hover:bg-brand-800 disabled:opacity-50 cursor-pointer"
      >
        {status === "submitting" ? (
          <span className="inline-flex items-center gap-2">
            <svg className="size-4 animate-spin" viewBox="0 0 24 24" fill="none">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            Gönderiliyor...
          </span>
        ) : (
          "Mesajı Gönder"
        )}
      </button>
    </form>
  );
}
