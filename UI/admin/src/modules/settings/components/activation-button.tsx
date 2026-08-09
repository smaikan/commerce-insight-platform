"use client";

import { useActionState } from "react";
import { setShippingMethodActivationAction, setTaxRateActivationAction } from "@/modules/settings/actions";
import { initialSettingsActionState } from "@/modules/settings/types";

// Burada iki ayar kaynağının belgeli aktiflik endpoint'lerini aynı dar etkileşimde çalıştırıyorum.
export function ActivationButton({ kind, id, isActive }: { kind: "shipping" | "tax"; id: string; isActive: boolean }) {
  const action = kind === "shipping" ? setShippingMethodActivationAction : setTaxRateActivationAction;
  const [state, formAction, pending] = useActionState(action.bind(null, id, !isActive), initialSettingsActionState);
  return (
    <div className="flex flex-col items-end gap-1.5">
      <form action={formAction}>
        <button
          type="submit"
          disabled={pending}
          aria-label={`${kind === "shipping" ? "Kargo yöntemi" : "Vergi oranı"} ${isActive ? "pasifleştir" : "etkinleştir"}`}
          className="min-h-9 rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60"
        >
          {pending ? "Güncelleniyor…" : isActive ? "Pasifleştir" : "Etkinleştir"}
        </button>
      </form>
      {state.status !== "idle" ? <p role="status" className={`max-w-48 text-right text-xs ${state.status === "error" ? "text-danger" : "text-success"}`}>{state.message}</p> : null}
    </div>
  );
}
