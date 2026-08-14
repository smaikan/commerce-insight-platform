import type { AccountActionState } from "@/modules/account/contracts";

// Burada kalıcı işlem sonucunu yalnız renge dayanmadan erişilebilir bir canlı bölgede açıklıyorum.
export function ActionFeedback({ state }: { state: AccountActionState }) {
  if (state.status === "idle" || !state.message) return null;
  return (
    <p
      role={state.status === "error" ? "alert" : "status"}
      className={`mt-4 border px-4 py-3 text-sm leading-6 ${
        state.status === "error"
          ? "border-danger/30 bg-danger/5 text-danger"
          : "border-success/30 bg-success/5 text-success"
      }`}
    >
      {state.message}
    </p>
  );
}
