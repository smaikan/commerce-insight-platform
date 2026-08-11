export type ApiProblem = {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;
  timestamp?: string;
  errors?: Record<string, string[]>;
};

// Burada API hatasını sunucu akışında taşıyıp yalnız güvenli ProblemDetails alanlarını koruyorum.
export class ApiError extends Error {
  readonly problem: ApiProblem;

  constructor(problem: ApiProblem) {
    super(problem.detail || problem.title);
    this.name = "ApiError";
    this.problem = problem;
  }
}

// Burada bilinmeyen hata gövdelerini güvenli ve serileştirilebilir ProblemDetails biçimine dönüştürüyorum.
export function normalizeApiProblem(status: number, value: unknown): ApiProblem {
  if (!value || typeof value !== "object") {
    return { title: "İstek tamamlanamadı", status };
  }

  const source = value as Record<string, unknown>;
  return {
    title: typeof source.title === "string" ? source.title : "İstek tamamlanamadı",
    status: typeof source.status === "number" ? source.status : status,
    detail: typeof source.detail === "string" ? source.detail : undefined,
    type: typeof source.type === "string" ? source.type : undefined,
    instance: typeof source.instance === "string" ? source.instance : undefined,
    code: typeof source.code === "string" ? source.code : undefined,
    traceId: typeof source.traceId === "string" ? source.traceId : undefined,
    timestamp: typeof source.timestamp === "string" ? source.timestamp : undefined,
    errors: isValidationErrors(source.errors) ? source.errors : undefined,
  };
}

// Burada doğrulama hatalarının beklenen alan-dizi yapısında olduğunu kontrol ediyorum.
function isValidationErrors(value: unknown): value is Record<string, string[]> {
  return Boolean(
    value &&
      typeof value === "object" &&
      Object.values(value).every(
        (messages) => Array.isArray(messages) && messages.every((message) => typeof message === "string"),
      ),
  );
}
