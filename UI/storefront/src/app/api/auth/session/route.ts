import { NextResponse } from "next/server";

import { hasAuthSessionCookie } from "@/lib/auth/cookies";

// Burada navbarın token değerini görmeden yalnız Storefront oturum çerezi varlığını öğrenebileceği private durum cevabını üretiyorum.
export async function GET() {
  return NextResponse.json(
    { authenticated: await hasAuthSessionCookie() },
    {
      headers: {
        "Cache-Control": "private, no-store",
        Vary: "Cookie",
      },
    },
  );
}
