import { NextResponse } from "next/server";

import { internalApiUrl } from "@/lib/api/client";
import { isProductPublicId } from "@/modules/favorites/request";

type ViewRouteContext = {
  params: Promise<{ productId: string }>;
};

export async function POST(_request: Request, context: ViewRouteContext) {
  const { productId } = await context.params;
  if (!productId || !isProductPublicId(productId)) {
    return new NextResponse(null, { status: 400 });
  }

  try {
    const upstream = await fetch(
      internalApiUrl(`/api/product-engagement/products/${encodeURIComponent(productId)}/activities`),
      {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          activityType: 0,
          quantity: 1,
        }),
        cache: "no-store",
        signal: AbortSignal.timeout(5_000),
      },
    );

    return new NextResponse(null, { status: upstream.ok ? 204 : upstream.status });
  } catch {
    return new NextResponse(null, { status: 204 });
  }
}
