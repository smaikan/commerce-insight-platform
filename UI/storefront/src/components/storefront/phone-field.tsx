"use client";

import { useState, type InputHTMLAttributes } from "react";

export function formatPhoneNumber(value: string) {
  let digits = value.replace(/\D/g, "");
  if (digits.startsWith("0")) digits = digits.substring(1);
  if (digits.length > 10) digits = digits.substring(0, 10);
  
  let res = "";
  if (digits.length > 0) res += "0(" + digits.substring(0, 3);
  if (digits.length >= 4) res += ") " + digits.substring(3, 6);
  if (digits.length >= 7) res += " " + digits.substring(6, 8);
  if (digits.length >= 9) res += " " + digits.substring(8, 10);
  return res;
}

export function PhoneField({ 
  name, 
  label, 
  error, 
  className = "", 
  variant = "checkout",
  defaultValue = "",
  ...props 
}: InputHTMLAttributes<HTMLInputElement> & { name: string; label: string; error?: string; variant?: "checkout" | "account" }) {
  const errorId = `${name}-error`;
  const [val, setVal] = useState(formatPhoneNumber(String(defaultValue)));

  const labelClass = variant === "checkout" 
    ? "mb-2 block text-sm font-semibold text-ink" 
    : "block text-xs font-bold text-ink";
    
  const inputClass = variant === "checkout"
    ? "focus-ring min-h-12 w-full rounded-lg border border-line bg-surface px-3 text-sm text-ink placeholder:text-ink-muted/70 aria-[invalid=true]:border-danger"
    : "focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-3 text-sm font-normal text-ink placeholder:text-ink-muted/70";

  return (
    <label htmlFor={name} className={`block ${className} ${variant === "account" ? "text-xs font-bold text-ink" : ""}`}>
      {variant === "checkout" ? (
        <span className={labelClass}>{label}{props.required ? <span className="ml-1 text-danger" aria-hidden="true">*</span> : null}</span>
      ) : (
        label
      )}
      <input
        {...props}
        id={name}
        name={name}
        type="tel"
        inputMode="tel"
        value={val}
        onChange={(e) => setVal(formatPhoneNumber(e.target.value))}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        className={inputClass}
      />
      {error ? <span id={errorId} className="mt-1.5 block text-sm font-semibold text-danger">{error}</span> : null}
    </label>
  );
}
