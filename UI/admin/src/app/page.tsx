import { redirect } from "next/navigation";
import { readRefreshToken } from "@/lib/auth/cookies";
import { getOptionalAdminSession } from "@/lib/auth/session";

// Burada kök route'u yalnız doğrulanmış Admin oturumunda panele, diğer durumlarda güvenli auth akışına yönlendiriyorum.
export default async function Home() {
  if (await getOptionalAdminSession()) redirect("/dashboard");
  if (await readRefreshToken()) redirect("/api/auth/refresh?returnTo=%2Fdashboard");
  redirect("/login");
}
