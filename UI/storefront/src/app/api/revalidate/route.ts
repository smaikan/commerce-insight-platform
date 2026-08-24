import { createHash, timingSafeEqual } from "node:crypto";
import { revalidatePath, revalidateTag } from "next/cache";
import { NextRequest, NextResponse } from "next/server";

export const runtime = "nodejs";

const MINIMUM_SECRET_BYTES = 32;
const MAXIMUM_SECRET_BYTES = 512;
const ALLOWED_TAGS = new Set([
  "banners",
  "main-banner",
  "main-banner-mobile",
  "alt-banners",
  "products",
  "published-products",
  "product-seo-index",
  "store-settings",
  "published-product-types",
  "published-collections",
  "published-brands",
  "navigation",
  "brands",
  "collections",
  "product-types",
]);
const ALLOWED_PATHS = new Set(["/", "/products", "/categories", "/collections"]);

type RevalidationRequest = {
  tag?: string;
  path?: string;
};

function json(body: Record<string, unknown>, status = 200) {
  const response = NextResponse.json(body, { status });
  response.headers.set("Cache-Control", "no-store");
  return response;
}

function configuredSecret(): string | null {
  const secret = process.env.STOREFRONT_REVALIDATE_SECRET;
  if (!secret) return null;

  const byteLength = Buffer.byteLength(secret, "utf8");
  return byteLength >= MINIMUM_SECRET_BYTES &&
    byteLength <= MAXIMUM_SECRET_BYTES &&
    /^[\x21-\x7e]+$/.test(secret)
    ? secret
    : null;
}

// Burada uzunluk bilgisini de gizlemek için iki değerin SHA-256 özetlerini sabit sürede karşılaştırıyorum.
function hasValidSecret(providedSecret: string | null, expectedSecret: string): boolean {
  if (!providedSecret) return false;

  const providedDigest = createHash("sha256").update(providedSecret, "utf8").digest();
  const expectedDigest = createHash("sha256").update(expectedSecret, "utf8").digest();
  return timingSafeEqual(providedDigest, expectedDigest);
}

function parseTarget(value: unknown): RevalidationRequest | null {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;

  const body = value as Record<string, unknown>;
  if (Object.keys(body).some((key) => key !== "tag" && key !== "path")) return null;
  if (body.tag !== undefined && typeof body.tag !== "string") return null;
  if (body.path !== undefined && typeof body.path !== "string") return null;

  const tag = body.tag as string | undefined;
  const path = body.path as string | undefined;
  if (tag !== undefined && !ALLOWED_TAGS.has(tag)) return null;
  if (path !== undefined && !ALLOWED_PATHS.has(path)) return null;

  return { tag, path };
}

// Burada yalnız API'nin paylaşılan header anahtarıyla çağırabildiği, allowlist sınırındaki cache hedeflerini geçersiz kılıyorum.
export async function POST(request: NextRequest) {
  const expectedSecret = configuredSecret();
  if (!expectedSecret) {
    return json({ message: "Cache yenileme servisi yapılandırılmamış." }, 503);
  }

  if (!hasValidSecret(request.headers.get("x-revalidate-secret"), expectedSecret)) {
    return json({ message: "Yetkisiz istek." }, 401);
  }

  if (!request.headers.get("content-type")?.toLowerCase().startsWith("application/json")) {
    return json({ message: "JSON istek gövdesi gerekli." }, 415);
  }

  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return json({ message: "Geçersiz JSON gövdesi." }, 400);
  }

  const target = parseTarget(body);
  if (!target) {
    return json({ message: "Desteklenmeyen cache hedefi." }, 400);
  }

  try {
    if (target.tag) revalidateTag(target.tag, "default");
    if (target.path) revalidatePath(target.path, "page");

    if (!target.tag && !target.path) {
      for (const tag of ALLOWED_TAGS) revalidateTag(tag, "default");
      revalidatePath("/", "layout");
    }

    return json({
      revalidated: true,
      tag: target.tag ?? "all",
      path: target.path ?? "all",
      timestamp: Date.now(),
    });
  } catch {
    return json({ message: "Cache yenileme işlemi tamamlanamadı." }, 500);
  }
}
