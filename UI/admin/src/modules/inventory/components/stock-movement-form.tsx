"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useRef, useState } from "react";
import {
  createBulkStockMovementsAction,
  createProductStockMovementsAction,
} from "@/modules/inventory/actions";
import { runStockMovementAction } from "@/modules/inventory/stock-movement-action-state";
import { getMovementFieldError } from "@/modules/inventory/stock-movement-form-data";
import {
  manualStockMovementTypeOptions,
  stockMovementDirectionOptions,
} from "@/modules/inventory/stock-movement-rules";
import { initialStockMovementActionState } from "@/modules/inventory/types";

type DraftMovement = {
  key: number;
  productVariantSku: string;
  type: number;
  direction: number;
  quantity: string;
  reason: string;
};

export type ProductStockVariantOption = {
  id: string;
  name: string;
  value: string;
  sku: string;
  stock: number;
};

type StockMovementRowProps = {
  row: DraftMovement;
  index: number;
  isPending: boolean;
  canRemove: boolean;
  fieldErrors?: Record<string, string[]>;
  variantOptions?: readonly ProductStockVariantOption[];
  onChange: (key: number, patch: Partial<DraftMovement>) => void;
  onRemove: (key: number) => void;
};

// Burada ilk hareket satırını izinli varsayılan satın alma girişiyle hazırlıyorum.
function initialRow(key: number, productVariantSku = ""): DraftMovement {
  return { key, productVariantSku, type: 10, direction: 1, quantity: "", reason: "" };
}

// Burada tek hareket satırının SKU, tür, yön, miktar ve açıklama alanlarını erişilebilir biçimde çiziyorum.
function StockMovementRow({
  row,
  index,
  isPending,
  canRemove,
  fieldErrors,
  variantOptions,
  onChange,
  onRemove,
}: StockMovementRowProps) {
  const typeOption = manualStockMovementTypeOptions.find((option) => option.value === row.type)
    ?? manualStockMovementTypeOptions[0];
  const skuError = getMovementFieldError(fieldErrors, index, "productVariantSku");
  const typeError = getMovementFieldError(fieldErrors, index, "type");
  const quantityError = getMovementFieldError(fieldErrors, index, "quantityDelta");
  const reasonError = getMovementFieldError(fieldErrors, index, "reason");
  const skuHelpId = `variant-sku-help-${row.key}`;
  const typeErrorId = `type-error-${row.key}`;
  const quantityErrorId = `quantity-error-${row.key}`;
  const reasonErrorId = `reason-error-${row.key}`;

  // Burada tür değiştiğinde yönü yeni türün izin verdiği ilk değere çekiyorum.
  function handleTypeChange(type: number) {
    const option = manualStockMovementTypeOptions.find((item) => item.value === type);
    onChange(row.key, { type, direction: option?.allowedDirections[0] ?? 1 });
  }

  return (
    <tr className="align-top">
      <td className="px-4 py-3">
        <label className="sr-only" htmlFor={`variant-sku-${row.key}`}>
          {index + 1}. satır varyant SKU
        </label>
        {variantOptions ? (
          <select
            id={`variant-sku-${row.key}`}
            value={row.productVariantSku}
            onChange={(event) => onChange(row.key, { productVariantSku: event.target.value })}
            required
            disabled={isPending || variantOptions.length === 0}
            aria-describedby={skuHelpId}
            aria-invalid={Boolean(skuError)}
            className="min-h-10 w-72 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 aria-[invalid=true]:border-danger"
          >
            <option value="">Varyant seçin</option>
            {variantOptions.map((variant) => (
              <option key={variant.id} value={variant.sku}>
                {variant.name}: {variant.value} · {variant.sku} · Stok {variant.stock}
              </option>
            ))}
          </select>
        ) : (
          <input
            id={`variant-sku-${row.key}`}
            name={`variant-sku-${row.key}`}
            type="text"
            autoComplete="off"
            maxLength={100}
            value={row.productVariantSku}
            onChange={(event) => onChange(row.key, { productVariantSku: event.target.value })}
            required
            disabled={isPending}
            placeholder="Örn. TSHIRT-BLACK-M"
            aria-describedby={skuHelpId}
            aria-invalid={Boolean(skuError)}
            className="min-h-10 w-64 rounded-lg border border-border-strong bg-surface-strong px-3 font-mono text-sm text-foreground outline-none placeholder:font-sans placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-focus/30 aria-[invalid=true]:border-danger"
          />
        )}
        <p id={skuHelpId} className={`mt-1 text-xs ${skuError ? "text-danger" : "text-muted"}`}>
          {skuError ?? (variantOptions ? "Yalnız bu ürüne ait varyantlar" : "Varyantın benzersiz SKU değeri")}
        </p>
      </td>
      <td className="px-3 py-3">
        <label className="sr-only" htmlFor={`type-${row.key}`}>
          {index + 1}. satır hareket türü
        </label>
        <select
          id={`type-${row.key}`}
          value={row.type}
          onChange={(event) => handleTypeChange(Number(event.target.value))}
          disabled={isPending}
          aria-describedby={typeError ? typeErrorId : undefined}
          aria-invalid={Boolean(typeError)}
          className="min-h-10 min-w-44 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 aria-[invalid=true]:border-danger"
        >
          {manualStockMovementTypeOptions.map((option) => (
            <option key={option.value} value={option.value}>{option.label}</option>
          ))}
        </select>
        {typeError ? <p id={typeErrorId} className="mt-1 text-xs text-danger">{typeError}</p> : null}
      </td>
      <td className="px-3 py-3">
        <label className="sr-only" htmlFor={`direction-${row.key}`}>
          {index + 1}. satır yönü
        </label>
        <select
          id={`direction-${row.key}`}
          value={row.direction}
          onChange={(event) => onChange(row.key, { direction: Number(event.target.value) })}
          disabled={isPending}
          className="min-h-10 min-w-28 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30"
        >
          {stockMovementDirectionOptions
            .filter((option) => typeOption.allowedDirections.includes(option.value))
            .map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
      </td>
      <td className="px-3 py-3">
        <label className="sr-only" htmlFor={`quantity-${row.key}`}>
          {index + 1}. satır miktarı
        </label>
        <input
          id={`quantity-${row.key}`}
          name={`quantity-${row.key}`}
          type="number"
          inputMode="numeric"
          min="1"
          step="1"
          value={row.quantity}
          onChange={(event) => onChange(row.key, { quantity: event.target.value })}
          required
          disabled={isPending}
          aria-describedby={quantityError ? quantityErrorId : undefined}
          aria-invalid={Boolean(quantityError)}
          className="min-h-10 w-24 rounded-lg border border-border-strong bg-surface-strong px-3 text-right tabular-nums text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 aria-[invalid=true]:border-danger"
        />
        {quantityError ? <p id={quantityErrorId} className="mt-1 text-xs text-danger">{quantityError}</p> : null}
      </td>
      <td className="px-3 py-3">
        <label className="sr-only" htmlFor={`reason-${row.key}`}>
          {index + 1}. satır açıklaması
        </label>
        <input
          id={`reason-${row.key}`}
          name={`reason-${row.key}`}
          type="text"
          maxLength={500}
          value={row.reason}
          onChange={(event) => onChange(row.key, { reason: event.target.value })}
          disabled={isPending}
          placeholder="Opsiyonel açıklama"
          aria-describedby={reasonError ? reasonErrorId : undefined}
          aria-invalid={Boolean(reasonError)}
          className="min-h-10 w-64 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-focus/30 aria-[invalid=true]:border-danger"
        />
        {reasonError ? <p id={reasonErrorId} className="mt-1 text-xs text-danger">{reasonError}</p> : null}
      </td>
      <td className="px-3 py-3">
        <button
          type="button"
          onClick={() => onRemove(row.key)}
          disabled={!canRemove || isPending}
          aria-label={`${index + 1}. hareket satırını kaldır`}
          className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-2.5 text-sm font-semibold text-muted hover:border-danger/35 hover:text-danger disabled:cursor-not-allowed disabled:opacity-50"
        >
          Kaldır
        </button>
      </td>
    </tr>
  );
}

// Burada atomik toplu hareket formunu, kullanıcı taslağını koruyan dar bir istemci sınırında yönetiyorum.
export function StockMovementForm({
  productId,
  variantOptions,
  disabled = false,
}: {
  productId?: string;
  variantOptions?: readonly ProductStockVariantOption[];
  disabled?: boolean;
} = {}) {
  const router = useRouter();
  const initialSku = variantOptions?.[0]?.sku || "";
  const action = useMemo(
    () => productId
      ? createProductStockMovementsAction.bind(null, productId)
      : createBulkStockMovementsAction,
    [productId],
  );
  const [rows, setRows] = useState<DraftMovement[]>([initialRow(1, initialSku)]);
  const [state, setState] = useState(initialStockMovementActionState);
  const [isPending, setPending] = useState(false);
  const submittingRef = useRef(false);
  const embedded = Boolean(productId);
  const productDraftLockId = "product-stock-movement-draft-lock";

  // Burada API sonucu gelir gelmez beklemeyi kapatıp başarıyı gösteriyor, authoritative görünümü arka planda yeniliyorum.
  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (disabled || submittingRef.current) return;
    submittingRef.current = true;
    setState(initialStockMovementActionState);
    const formData = new FormData(event.currentTarget);

    void runStockMovementAction(action, formData, setPending)
      .then((result) => {
        setState(result);
        if (result.status !== "success" || !result.movementCount) return;
        if (embedded) router.refresh();
        else router.push(`/inventory/stock-movements?created=${result.movementCount}`);
      })
      .finally(() => {
        submittingRef.current = false;
      });
  }

  // Burada yalnız hedef satırın taslağını değiştirerek diğer satırları koruyorum.
  function updateRow(key: number, patch: Partial<DraftMovement>) {
    setRows((current) => current.map((row) => row.key === key ? { ...row, ...patch } : row));
  }

  // Burada beş yüz satır sınırını aşmadan benzersiz anahtarlı yeni bir taslak ekliyorum.
  function addRow() {
    setRows((current) => current.length >= 500
      ? current
      : [...current, initialRow(Math.max(...current.map((row) => row.key)) + 1, initialSku)]);
  }

  // Burada formda daima en az bir hareket satırı kalmasını sağlıyorum.
  function removeRow(key: number) {
    setRows((current) => current.length === 1 ? current : current.filter((row) => row.key !== key));
  }

  return (
    <form onSubmit={handleSubmit} aria-busy={isPending} className={embedded ? "mt-4 space-y-4" : "space-y-5"}>
      <input type="hidden" name="movements" value={JSON.stringify(rows)} />

      {state.status === "error" ? (
        <div role="alert" className="rounded-xl border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger">
          <p className="font-semibold">Hareketler kaydedilemedi</p>
          <p className="mt-1">{state.message}</p>
          {state.traceId ? <p className="mt-2 font-mono text-xs">Takip: {state.traceId}</p> : null}
        </div>
      ) : null}

      {embedded && state.status === "success" ? (
        <div role="status" className="rounded-lg border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-900">
          {state.message}
        </div>
      ) : null}

      <section
        aria-labelledby="movement-lines-title"
        aria-describedby={embedded && disabled ? productDraftLockId : undefined}
        className="overflow-hidden rounded-xl border border-border bg-surface"
      >
        <div className="flex flex-col gap-3 border-b border-border bg-surface-subtle px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-5">
          <div>
            <h2 id="movement-lines-title" className="text-base font-semibold text-foreground">{embedded ? "Stok hareketi ekle" : "Hareket satırları"}</h2>
            <p className="mt-1 text-sm text-muted">{embedded ? "Bu ürünün varyantları için giriş, çıkış veya sayım düzeltmesi kaydedin." : "Varyantı SKU ile seçin. En fazla 500 satır tek atomik işlemde kaydedilir."}</p>
            {embedded && disabled ? <span id={productDraftLockId} className="sr-only">Ürün formunda kaydedilmemiş değişiklikler bulunduğu için stok hareketi kontrolleri geçici olarak kullanılamıyor.</span> : null}
          </div>
          <button
            type="button"
            onClick={addRow}
            disabled={rows.length >= 500 || isPending || disabled}
            className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60"
          >
            Satır ekle
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[940px] border-collapse text-left text-sm">
            <thead className="border-b border-border bg-surface-subtle/60 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
              <tr>
                <th scope="col" className="px-4 py-2.5">Varyant SKU</th>
                <th scope="col" className="px-3 py-2.5">Tür</th>
                <th scope="col" className="px-3 py-2.5">Yön</th>
                <th scope="col" className="px-3 py-2.5">Miktar</th>
                <th scope="col" className="px-3 py-2.5">Açıklama</th>
                <th scope="col" className="w-12 px-3 py-2.5"><span className="sr-only">Satırı kaldır</span></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/80">
              {rows.map((row, index) => (
                <StockMovementRow
                  key={row.key}
                  row={row}
                  index={index}
                  isPending={isPending || disabled}
                  canRemove={rows.length > 1}
                  fieldErrors={state.fieldErrors}
                  variantOptions={variantOptions}
                  onChange={updateRow}
                  onRemove={removeRow}
                />
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {!embedded ? <section aria-labelledby="movement-rule-title" className="rounded-xl border border-border bg-surface px-4 py-4 sm:px-5">
        <h2 id="movement-rule-title" className="text-base font-semibold text-foreground">İşlem kuralları</h2>
        <ul className="mt-2 space-y-1.5 text-sm leading-6 text-muted">
          <li>Satış, iptal ve iade gibi sistem kaynaklı hareketler bu formdan oluşturulamaz.</li>
          <li>Miktar pozitif girilir; seçilen yön API’ye imzalı hareket olarak gönderilir.</li>
          <li>SKU bulunamazsa, stok eksiye düşerse veya bir satır geçersizse hiçbir hareket kaydedilmez.</li>
        </ul>
      </section> : null}

      <div className="flex flex-col-reverse gap-3 border-t border-border pt-4 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-muted">{rows.length} / 500 hareket satırı</p>
        <div className="flex flex-wrap gap-2">
          {!embedded ? <Link
            href="/inventory/stock-movements"
            className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle"
          >
            Vazgeç
          </Link> : null}
          <button
            type="submit"
            disabled={isPending || disabled || Boolean(variantOptions && variantOptions.length === 0)}
            className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isPending ? "Kaydediliyor…" : "Hareketleri kaydet"}
          </button>
        </div>
      </div>
    </form>
  );
}
