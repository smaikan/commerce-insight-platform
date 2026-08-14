import "server-only";

import { redirect } from "next/navigation";

import { ApiError } from "@/lib/api/problem";

// Burada 401 alan hesap okumalarını cookie yazabilen refresh sınırına yönlendirip diğer hataları koruyorum.
export async function withAccountSession<T>(returnTo: string, operation: () => Promise<T>): Promise<T> {
  try {
    return await operation();
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 401) {
      redirect(`/api/auth/refresh?returnTo=${encodeURIComponent(returnTo)}`);
    }
    throw error;
  }
}
