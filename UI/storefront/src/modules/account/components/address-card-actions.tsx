"use client";

import { useState, useTransition } from "react";

import { deleteAddressAction, setDefaultAddressAction } from "@/modules/account/actions";
import { ActionFeedback } from "@/modules/account/components/action-feedback";
import { INITIAL_ACCOUNT_ACTION_STATE, type AccountActionState } from "@/modules/account/contracts";

// Burada varsayılan yapma ve silme işlemlerini aynı kartta açık onay ve kalıcı sonuç mesajıyla yönetiyorum.
export function AddressCardActions({ id, isDefault }: { id: string; isDefault: boolean }) {
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [state, setState] = useState<AccountActionState>(INITIAL_ACCOUNT_ACTION_STATE);
  const [pending, startTransition] = useTransition();

  function run(operation: () => Promise<AccountActionState>) {
    startTransition(() => void operation().then(setState));
  }

  return (
    <div className="mt-4 border-t border-line pt-4">
      <div className="flex flex-wrap items-center gap-2">
        {!isDefault ? <button type="button" disabled={pending} onClick={() => run(() => setDefaultAddressAction(id))} className="focus-ring min-h-10 border border-line px-3 text-xs font-bold text-brand-700 hover:bg-surface-subtle disabled:cursor-wait">Varsayılan yap</button> : null}
        {!confirmDelete ? (
          <button type="button" disabled={pending} onClick={() => setConfirmDelete(true)} className="focus-ring min-h-10 px-3 text-xs font-bold text-danger hover:bg-danger/5 disabled:cursor-wait">Sil</button>
        ) : (
          <div className="flex flex-wrap items-center gap-2" role="group" aria-label="Adresi silme onayı">
            <span className="text-xs font-semibold text-ink">Bu adres silinsin mi?</span>
            <button type="button" disabled={pending} onClick={() => run(() => deleteAddressAction(id))} className="focus-ring min-h-10 bg-danger px-3 text-xs font-bold text-white disabled:cursor-wait">Evet, sil</button>
            <button type="button" disabled={pending} onClick={() => setConfirmDelete(false)} className="focus-ring min-h-10 border border-line px-3 text-xs font-bold text-ink">Vazgeç</button>
          </div>
        )}
      </div>
      <ActionFeedback state={state} />
    </div>
  );
}
