"use client";

import { useState } from "react";
import { useFormStatus } from "react-dom";

type FieldProps = {
  id: string;
  label: string;
  name: string;
  type?: "text" | "email" | "tel";
  autoComplete: string;
  defaultValue?: string;
  error?: string;
  required?: boolean;
  maxLength?: number;
};

// Burada ortak metin alanını sabit label, belirgin hata ve en az 44 piksel etkileşim yüksekliğiyle sunuyorum.
export function AuthField({ id, label, error, required = false, ...props }: FieldProps) {
  const errorId = `${id}-error`;
  return (
    <div>
      <label htmlFor={id} className="mb-2 block text-sm font-bold text-ink">{label}{required ? <span className="text-danger" aria-hidden="true"> *</span> : null}</label>
      <input
        {...props}
        id={id}
        required={required}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        className="focus-ring min-h-12 w-full border border-line bg-surface px-3.5 text-base text-ink placeholder:text-ink-muted/70 focus:border-brand-600 sm:text-sm"
      />
      {error ? <p id={errorId} className="mt-1.5 text-sm font-medium text-danger">{error}</p> : null}
    </div>
  );
}

type PasswordFieldProps = {
  id: string;
  label: string;
  name: string;
  autoComplete: "current-password" | "new-password";
  error?: string;
  hint?: string;
};

// Burada şifreyi varsayılan olarak gizli tutup erişilebilir bir kontrolle kullanıcı isteğine göre görünür yapıyorum.
export function PasswordField({ id, label, name, autoComplete, error, hint }: PasswordFieldProps) {
  const [visible, setVisible] = useState(false);
  const errorId = `${id}-error`;
  const hintId = `${id}-hint`;
  return (
    <div>
      <label htmlFor={id} className="mb-2 block text-sm font-bold text-ink">{label}<span className="text-danger" aria-hidden="true"> *</span></label>
      <div className="relative">
        <input
          id={id}
          name={name}
          type={visible ? "text" : "password"}
          autoComplete={autoComplete}
          required
          minLength={autoComplete === "new-password" ? 6 : undefined}
          maxLength={128}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? errorId : hint ? hintId : undefined}
          className="focus-ring min-h-12 w-full border border-line bg-surface pr-20 pl-3.5 text-base text-ink focus:border-brand-600 sm:text-sm"
        />
        <button type="button" onClick={() => setVisible((current) => !current)} aria-controls={id} aria-pressed={visible} className="focus-ring absolute inset-y-1 right-1 min-w-16 px-2 text-xs font-bold text-brand-700 hover:text-brand-950">
          {visible ? "Gizle" : "Göster"}
        </button>
      </div>
      {error ? <p id={errorId} className="mt-1.5 text-sm font-medium text-danger">{error}</p> : hint ? <p id={hintId} className="mt-1.5 text-xs leading-5 text-ink-muted">{hint}</p> : null}
    </div>
  );
}

// Burada form gönderilirken çift isteği engelleyip durum metnini ekran okuyucuya da yansıtıyorum.
export function SubmitButton({ idleLabel, pendingLabel }: { idleLabel: string; pendingLabel: string }) {
  const { pending } = useFormStatus();
  return (
    <button type="submit" disabled={pending} aria-disabled={pending} className="focus-ring flex min-h-12 w-full items-center justify-center bg-brand-950 px-5 text-sm font-black tracking-wide text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-65">
      {pending ? pendingLabel : idleLabel}
    </button>
  );
}

// Burada Google seçeneğini görsel olarak hazır tutup entegrasyon bitene kadar etkileşim ve üçüncü taraf yüklemesini kapatıyorum.
export function GoogleDevelopmentButton() {
  return (
    <button type="button" disabled aria-describedby="google-development-note" className="flex min-h-12 w-full cursor-not-allowed items-center justify-center gap-3 border border-line bg-surface-subtle px-4 text-sm font-bold text-ink-muted opacity-85">
      <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5" fill="none">
        <path fill="#4285F4" d="M21.6 12.2c0-.7-.1-1.4-.2-2H12v3.8h5.4a4.6 4.6 0 0 1-2 3v2.5h3.2c1.9-1.8 3-4.3 3-7.3Z" />
        <path fill="#34A853" d="M12 22c2.7 0 5-.9 6.6-2.5L15.4 17c-.9.6-2 .9-3.4.9-2.6 0-4.8-1.8-5.6-4.1H3.1v2.6A10 10 0 0 0 12 22Z" />
        <path fill="#FBBC05" d="M6.4 13.8A6 6 0 0 1 6.1 12c0-.6.1-1.2.3-1.8V7.6H3.1A10 10 0 0 0 2 12c0 1.6.4 3.1 1.1 4.4l3.3-2.6Z" />
        <path fill="#EA4335" d="M12 6.1c1.5 0 2.8.5 3.8 1.5l2.9-2.8A9.7 9.7 0 0 0 12 2a10 10 0 0 0-8.9 5.6l3.3 2.6c.8-2.4 3-4.1 5.6-4.1Z" />
      </svg>
      Google ile devam et
      <span id="google-development-note" className="border-l border-line pl-3 text-[0.65rem] tracking-wider uppercase">Geliştirme aşamasında</span>
    </button>
  );
}
