"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { ConfirmDialog } from "@/lib/admin/components/confirm-dialog";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";

type EntityDeleteButtonProps = {
  entityName: string;
  title: string;
  description: string;
  confirmLabel: string;
  onDelete: () => Promise<AdminMutationResult>;
};

// Burada liste kayıtlarının silme isteğini ortak onay, hata ve yenileme davranışıyla yönetiyorum.
export function EntityDeleteButton({ entityName, title, description, confirmLabel, onDelete }: EntityDeleteButtonProps) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, setPending] = useState(false);
  const [result, setResult] = useState<AdminMutationResult>();

  // Burada onaylanan silme işlemini çalıştırıp başarılı sonucu listeye yansıtıyorum.
  const confirmDelete = async () => {
    if (pending) return;
    setPending(true);
    const response = await onDelete();
    setPending(false);
    setResult(response);
    if (response.status === "success") {
      setOpen(false);
      // Burada yeni bir route'a geçerken ikinci bir refresh başlatmıyor, yerinde kalan işlemi ise güncel veriden yeniden okuyorum.
      if (response.redirectHref) router.push(response.redirectHref);
      else router.refresh();
    }
  };

  return (
    <>
      <button type="button" onClick={() => { setResult(undefined); setOpen(true); }} aria-label={`${entityName} kaydını sil`} className="inline-flex min-h-9 cursor-pointer items-center rounded-lg border border-danger/30 bg-surface-strong px-3 text-xs font-semibold text-danger transition-colors hover:bg-danger/10">Sil</button>
      <ConfirmDialog open={open} title={title} description={description} confirmLabel={confirmLabel} pending={pending} error={result?.status === "error" ? result.message : undefined} onCancel={() => { if (!pending) { setOpen(false); setResult(undefined); } }} onConfirm={confirmDelete} />
    </>
  );
}
