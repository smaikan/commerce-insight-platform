export type LoginActionState = {
  status: "idle" | "error";
  message?: string;
  email?: string;
  fieldErrors?: Record<string, string[]>;
  traceId?: string;
};

export const initialLoginActionState: LoginActionState = { status: "idle" };
