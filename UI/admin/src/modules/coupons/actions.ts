"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { createCoupon, setCouponActivation, updateCoupon } from "@/modules/coupons/api";
import { parseCouponForm } from "@/modules/coupons/form-data";
import type { CouponActionState } from "@/modules/coupons/types";

// Burada yeni kuponu doğrulayıp başarılı sonuçta güncel listeye dönüyorum.
export async function createCouponAction(_previousState: CouponActionState, formData: FormData): Promise<CouponActionState> {
  const session = await actionSession();
  if (!session.ok) return session.state;
  const parsed = parseCouponForm(formData);
  if (!parsed.ok) return { status: "error", message: parsed.message, fieldErrors: parsed.fieldErrors };
  try {
    await createCoupon(parsed.value, session.value);
  } catch (error) {
    return actionError(error, "Kupon oluşturulamadı");
  }
  revalidatePath("/coupons");
  redirect("/coupons?created=1");
}

// Burada listede seçilen kuponu tam güncelleme sözleşmesiyle kaydediyorum.
export async function updateCouponAction(id: string, _previousState: CouponActionState, formData: FormData): Promise<CouponActionState> {
  const session = await actionSession();
  if (!session.ok) return session.state;
  const parsed = parseCouponForm(formData);
  if (!parsed.ok) return { status: "error", message: parsed.message, fieldErrors: parsed.fieldErrors };
  try {
    await updateCoupon(id, parsed.value, session.value);
  } catch (error) {
    return actionError(error, "Kupon güncellenemedi");
  }
  revalidatePath("/coupons");
  redirect("/coupons?updated=1");
}

// Burada tek tık aktiflik değişimini dar activation endpoint'ine yönlendiriyorum.
export async function setCouponActivationAction(id: string, isActive: boolean) {
  const session = await actionSession();
  if (!session.ok) return session.state;
  try {
    await setCouponActivation(id, isActive, session.value);
  } catch (error) {
    return actionError(error, "Kupon durumu değiştirilemedi");
  }
  revalidatePath("/coupons");
  return { status: "idle" } satisfies CouponActionState;
}

// Burada mutation öncesi yönetici oturumunu formun koruyabileceği sonuca çeviriyorum.
async function actionSession(): Promise<{ ok: true; value: Awaited<ReturnType<typeof requireAdminActionSession>> } | { ok: false; state: CouponActionState }> {
  try {
    return { ok: true, value: await requireAdminActionSession() };
  } catch (error) {
    return { ok: false, state: actionError(error, "Yönetici oturumu doğrulanamadı") };
  }
}

// Burada ProblemDetails hatasını kullanıcı için izlenebilir ve güvenli forma dönüştürüyorum.
function actionError(error: unknown, prefix: string): CouponActionState {
  if (error instanceof ApiError) {
    const message = error.problem.status === 403
      ? "Bu işlem yalnızca aktif yönetici hesaplarına açıktır."
      : error.problem.status === 401
        ? "Oturumunuz sona erdi. Form veriniz korunuyor; yeniden giriş yapın."
        : `${prefix}: ${error.problem.detail || error.problem.title}`;
    return { status: "error", message, traceId: error.problem.traceId, fieldErrors: error.problem.errors };
  }
  return { status: "error", message: `${prefix}. Lütfen tekrar deneyin.` };
}
