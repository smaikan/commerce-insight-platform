import type { AuthFieldErrors, AuthFormValues } from "@/modules/auth/validation";

export type AuthActionState = {
  status: "idle" | "error";
  revision: number;
  message?: string;
  fieldErrors?: AuthFieldErrors;
  values?: AuthFormValues;
};

// Burada istemci formunun başlangıç durumunu Server Action dosyasından ayrı tutarak Next.js export sınırını koruyorum.
export const initialAuthState: AuthActionState = { status: "idle", revision: 0 };
