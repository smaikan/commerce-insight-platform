export type ApiProblem = {
  title: string;
  status: number;
  detail?: string;
  code?: string;
  traceId?: string;
  retryAfter?: string;
  errors?: Record<string, string[]>;
};

// Burada API hatasını server katmanında durum kodu ve güvenli ProblemDetails alanlarıyla taşıyorum.
export class ApiError extends Error {
  readonly problem: ApiProblem;

  constructor(problem: ApiProblem) {
    super(problem.detail || problem.title);
    this.name = "ApiError";
    this.problem = problem;
  }
}

// Burada bilinmeyen hata gövdelerini güvenli ve tek biçimli bir ProblemDetails modeline dönüştürüyorum.
export function normalizeApiProblem(value: unknown, status: number, retryAfter?: string | null): ApiProblem {
  if (value && typeof value === "object") {
    const candidate = value as Record<string, unknown>;
    return {
      title: typeof candidate.title === "string" ? candidate.title : "İstek tamamlanamadı",
      status,
      detail: typeof candidate.detail === "string" ? candidate.detail : undefined,
      code: typeof candidate.code === "string" ? candidate.code : undefined,
      traceId: typeof candidate.traceId === "string" ? candidate.traceId : undefined,
      retryAfter: retryAfter || undefined,
      errors: isFieldErrorMap(candidate.errors) ? candidate.errors : undefined,
    };
  }

  return {
    title: "İstek tamamlanamadı",
    status,
    detail: "Sunucu beklenmeyen bir yanıt döndürdü.",
    retryAfter: retryAfter || undefined,
  };
}

// Burada yalnız string dizileri taşıyan doğrulama hata sözlüğünü istemciye aktarıyorum.
function isFieldErrorMap(value: unknown): value is Record<string, string[]> {
  return Boolean(
    value &&
      typeof value === "object" &&
      Object.values(value).every(
        (messages) => Array.isArray(messages) && messages.every((message) => typeof message === "string"),
      ),
  );
}
