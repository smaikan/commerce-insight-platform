"use server";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { promoteToAdmin, registerManager } from "@/modules/managers/api";
import type { ManagerActionState, RegisterManagerRequest } from "@/modules/managers/types";

// Burada kullanıcıyı oluşturup rolü Admin'e yükselten iki aşamalı akışı açık hata durumuyla çalıştırıyorum.
export async function createManagerAction(_state: ManagerActionState, formData: FormData): Promise<ManagerActionState> {
  let session; try { session = await requireAdminActionSession(); } catch { return { status: "error", message: "Yönetici oturumu doğrulanamadı." }; }
  const firstName = String(formData.get("firstName") || "").trim(); const lastName = String(formData.get("lastName") || "").trim(); const email = String(formData.get("email") || "").trim(); const password = String(formData.get("password") || ""); const phoneNumber = String(formData.get("phoneNumber") || "").trim();
  if (!firstName || !lastName || !email || !password) return { status: "error", message: "Ad, soyad, e-posta ve parola zorunludur." };
  const payload: RegisterManagerRequest = { firstName, lastName, email, password, phoneNumber: phoneNumber || null };
  let userId: string;
  try { userId = (await registerManager(payload, session)).user.id; } catch (error) { return errorState(error, "Yönetici hesabı oluşturulamadı"); }
  try { await promoteToAdmin(userId, session); } catch (error) { const result = errorState(error, "Rol yükseltilemedi"); return { ...result, status: "partial", message: `Kullanıcı oluşturuldu ancak Admin rolü atanamadı. Kullanıcı şu an müşteri rolündedir. ${result.message}` }; }
  revalidatePath("/managers"); redirect("/managers?created=1");
}

function errorState(error: unknown, prefix: string): ManagerActionState { if (error instanceof ApiError) return { status: "error", message: `${prefix}: ${error.problem.detail || error.problem.title}`, traceId: error.problem.traceId, fieldErrors: error.problem.errors }; return { status: "error", message: `${prefix}.` }; }
