import "server-only";

import { NextResponse } from "next/server";

import { internalApiUrl } from "@/lib/api/client";
import { normalizeApiProblem, type ApiProblem } from "@/lib/api/problem";
import { readAccessToken } from "@/lib/auth/cookies";
import { siteConfig } from "@/lib/site-config";
import type { ContactSubmissionReceipt, ContactSubmissionRequest } from "@/modules/contact/types";

const CONTACT_UPSTREAM_TIMEOUT_MS = 15_000;

function isReceipt(value: unknown): value is ContactSubmissionReceipt {
  if (!value || typeof value !== "object") return false;
  const source = value as Record<string, unknown>;
  return typeof source.referenceNumber === "string" && source.referenceNumber.startsWith("CNT-") &&
    typeof source.submittedAt === "string" && !Number.isNaN(Date.parse(source.submittedAt));
}

export function contactProblemResponse(
  status: number,
  title: string,
  detail: string,
  code: string,
  errors?: Record<string, string[]>,
): NextResponse {
  return NextResponse.json(
    { status, title, detail, code, ...(errors ? { errors } : {}) },
    {
      status,
      headers: {
        "Cache-Control": "private, no-store",
        "Content-Type": "application/problem+json",
      },
    },
  );
}

function upstreamProblemResponse(problem: ApiProblem): NextResponse {
  const headers = new Headers({
    "Cache-Control": "private, no-store",
    "Content-Type": "application/problem+json",
  });
  if (problem.retryAfter) headers.set("Retry-After", problem.retryAfter);
  return NextResponse.json(problem, { status: problem.status, headers });
}

// Burada contact submission'ı opsiyonel müşteri JWT'si ve yalnız allowlist edilmiş güvenlik header'larıyla upstream API'ye iletiyorum.
export async function forwardContactSubmission(
  body: ContactSubmissionRequest,
  idempotencyKey: string,
  turnstileToken?: string,
): Promise<NextResponse> {
  const accessToken = await readAccessToken();
  const headers = new Headers({
    Accept: "application/json",
    "Content-Type": "application/json",
    "Idempotency-Key": idempotencyKey,
    Origin: new URL(siteConfig.url).origin,
  });
  if (turnstileToken) headers.set("X-Turnstile-Token", turnstileToken);
  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);

  let upstream: Response;
  try {
    upstream = await fetch(internalApiUrl("/api/contact-messages"), {
      method: "POST",
      headers,
      body: JSON.stringify(body),
      cache: "no-store",
      signal: AbortSignal.timeout(CONTACT_UPSTREAM_TIMEOUT_MS),
    });
  } catch {
    return contactProblemResponse(
      503,
      "İletişim servisine ulaşılamıyor",
      "Talebiniz şu anda alınamadı. Form içeriğiniz korunuyor; kısa bir süre sonra tekrar deneyin.",
      "contact_unavailable",
    );
  }

  const contentType = upstream.headers.get("content-type") || "";
  const value = contentType.includes("json") ? await upstream.json().catch(() => null) : null;
  if (!upstream.ok) {
    return upstreamProblemResponse(normalizeApiProblem(
      upstream.status,
      value,
      upstream.headers.get("Retry-After"),
    ));
  }

  if (upstream.status !== 202 || !isReceipt(value)) {
    return contactProblemResponse(
      502,
      "İletişim yanıtı doğrulanamadı",
      "Talebinizin sonucu doğrulanamadı. Aynı form içeriğiyle tekrar deneyebilirsiniz.",
      "contact_invalid_response",
    );
  }

  return NextResponse.json(value, {
    status: 202,
    headers: {
      "Cache-Control": "private, no-store",
      "Content-Type": "application/json",
    },
  });
}
