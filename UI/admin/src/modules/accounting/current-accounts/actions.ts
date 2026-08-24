"use server";

import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { createCurrentAccount, updateCurrentAccount } from "@/modules/accounting/current-accounts/api";
import { parseCurrentAccountForm } from "@/modules/accounting/current-accounts/form-data";
import type { CurrentAccountFormState } from "@/modules/accounting/current-accounts/types";

export async function saveCurrentAccountAction(id: string | undefined, _previous: CurrentAccountFormState, formData: FormData): Promise<CurrentAccountFormState> {
  const parsed = parseCurrentAccountForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    const account = id
      ? await updateCurrentAccount(id, parsed.input, formData.get("isActive") === "on", session)
      : await createCurrentAccount(parsed.input, session);
    revalidatePath("/accounting");
    revalidatePath("/accounting/current-accounts");
    revalidatePath(`/accounting/current-accounts/${encodeURIComponent(account.id)}`);
    return { status: "success", message: id ? "Cari hesap güncellendi." : "Cari hesap oluşturuldu.", redirectHref: `/accounting/current-accounts/${encodeURIComponent(account.id)}?${id ? "updated" : "created"}=1` };
  } catch (error) {
    return currentAccountError(error, parsed.draft);
  }
}

function currentAccountError(error: unknown, draft: CurrentAccountFormState["draft"]): CurrentAccountFormState {
  if (!(error instanceof ApiError)) return { status: "error", message: "Cari hesap kaydedilemedi. Lütfen tekrar deneyin.", draft };
  const p = error.problem;
  const message = p.status === 401 ? "Oturumunuz sona erdi. Yeniden giriş yapın." : p.status === 403 ? "Bu işlem için yönetici yetkiniz yok." : p.status === 404 ? "Cari hesap artık bulunamıyor." : p.status === 409 && p.code === "concurrency_conflict" ? "Cari hesap başka bir yönetici tarafından değiştirildi. Sayfayı yenileyip güncel kayıt üzerinden tekrar deneyin." : p.status === 409 ? "Cari kodu veya bağlantılı bilgiler güncel bir kayıtla çakışıyor." : p.status === 429 ? p.retryAfter ? `İstek sınırına ulaşıldı. ${p.retryAfter} sonra tekrar deneyin.` : "İstek sınırına ulaşıldı. Bir süre sonra tekrar deneyin." : p.detail || p.title;
  return { status: "error", message, code: p.code, traceId: p.traceId, retryAfter: p.retryAfter, fieldErrors: p.errors, draft };
}
