export type AdminMutationResult = {
  status: "success" | "error";
  message: string;
  traceId?: string;
  redirectHref?: string;
  refresh?: boolean;
};
