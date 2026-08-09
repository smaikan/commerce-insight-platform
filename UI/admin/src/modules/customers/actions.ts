"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { updateCustomerRole, updateCustomerStatus } from "@/modules/customers/api";
import type { UserRole, UserStatus } from "@/modules/customers/types";

// Burada müşteri rolü değişikliğini tekrar doğrulanmış yönetici oturumuyla uyguluyorum.
export async function updateCustomerRoleAction(formData: FormData): Promise<void> {
  const customerId = readCustomerId(formData);
  const role = readRole(formData);
  if (!customerId || role === undefined) redirect("/customers");
  try {
    await updateCustomerRole(customerId, role, await requireAdminActionSession());
  } catch (error) {
    redirectWithResult(customerId, error);
  }
  revalidatePath("/customers");
  revalidatePath(`/customers/${customerId}`);
  redirect(`/customers/${encodeURIComponent(customerId)}?updated=role`);
}

// Burada müşteri hesap durumunu ayrı belgelenmiş komutla güncelliyorum.
export async function updateCustomerStatusAction(formData: FormData): Promise<void> {
  const customerId = readCustomerId(formData);
  const status = readStatus(formData);
  if (!customerId || status === undefined) redirect("/customers");
  try {
    await updateCustomerStatus(customerId, status, await requireAdminActionSession());
  } catch (error) {
    redirectWithResult(customerId, error);
  }
  revalidatePath("/customers");
  revalidatePath(`/customers/${customerId}`);
  redirect(`/customers/${encodeURIComponent(customerId)}?updated=status`);
}

// Burada formdaki public kullanıcı kimliğini boş veya çoklu değerlerden koruyorum.
function readCustomerId(formData: FormData): string | undefined {
  const value = formData.get("customerId");
  return typeof value === "string" && value ? value : undefined;
}

// Burada rol enum değerini sözleşmedeki iki değerle sınırlıyorum.
function readRole(formData: FormData): UserRole | undefined {
  const value = Number(formData.get("role"));
  return value === 1 || value === 2 ? value : undefined;
}

// Burada hesap durumu enum değerini sözleşmedeki allowlist üzerinden doğruluyorum.
function readStatus(formData: FormData): UserStatus | undefined {
  const value = Number(formData.get("status"));
  return value === 1 || value === 2 || value === 3 ? value : undefined;
}

// Burada 401, 403 ve son aktif yönetici çakışmasını detay ekranında anlaşılır durumlara yönlendiriyorum.
function redirectWithResult(customerId: string, error: unknown): never {
  const result = error instanceof ApiError && error.problem.status === 409
    ? "conflict"
    : error instanceof ApiError && error.problem.status === 403
      ? "forbidden"
      : "failed";
  redirect(`/customers/${encodeURIComponent(customerId)}?error=${result}`);
}
