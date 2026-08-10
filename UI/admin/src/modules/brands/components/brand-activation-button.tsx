"use client";

import { useActionState } from "react";
import { setBrandActivationAction } from "@/modules/brands/actions";
import { initialBrandActionState } from "@/modules/brands/types";

// Burada yalnız seçilen marka satırının aktiflik eylemini kilitleyip sonucunu yerinde duyuruyorum.
export function BrandActivationButton({ id, isActive, name }: { id: string; isActive: boolean; name: string }) {
  const [state, formAction, pending] = useActionState(
    setBrandActivationAction.bind(null, id, !isActive),
    initialBrandActionState,
  );

  return (
    <div className="flex flex-col items-end gap-1.5">
      <form action={formAction}>
        <button
          type="submit"
          disabled={pending}
          aria-label={`${name} markasını ${isActive ? "pasifleştir" : "etkinleştir"}`}
          className="min-h-9 rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60"
        >
          {pending ? "Güncelleniyor…" : isActive ? "Pasifleştir" : "Etkinleştir"}
        </button>
      </form>
      {state.status !== "idle" ? (
        <p role="status" className={`max-w-48 text-right text-xs ${state.status === "error" ? "text-danger" : "text-success"}`}>
          {state.message}
        </p>
      ) : null}
    </div>
  );
}
