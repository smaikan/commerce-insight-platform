"use client";

import { useState, type KeyboardEvent } from "react";
import type { Collection } from "@/modules/products/types";

export const MAX_PRODUCT_COLLECTIONS = 100;
export const MAX_COLLECTION_NAME_LENGTH = 150;

type CollectionSelectionResult = {
  selected: string[];
  message?: string;
  changed: boolean;
};

// Burada API'nin isimle eşleştirme davranışına uygun biçimde Türkçe büyük/küçük harf farkını seçim kimliğinden çıkarıyorum.
function collectionKey(name: string): string {
  return name.trim().toLocaleLowerCase("tr-TR");
}

// Burada checklist, yeni ad alanı ve rozet kaldırma eylemlerinin aynı benzersiz seçim kuralını kullanmasını sağlıyorum.
export function updateCollectionSelection(
  current: string[],
  name: string,
  selected: boolean,
): CollectionSelectionResult {
  const trimmed = name.trim();
  if (!trimmed) return { selected: current, message: "Önce bir koleksiyon adı yazın.", changed: false };
  if (trimmed.length > MAX_COLLECTION_NAME_LENGTH) {
    return { selected: current, message: `Koleksiyon adı en fazla ${MAX_COLLECTION_NAME_LENGTH} karakter olabilir.`, changed: false };
  }

  const key = collectionKey(trimmed);
  const existingIndex = current.findIndex((item) => collectionKey(item) === key);
  if (!selected) {
    if (existingIndex < 0) return { selected: current, changed: false };
    return { selected: current.filter((_, index) => index !== existingIndex), changed: true };
  }
  if (existingIndex >= 0) return { selected: current, message: "Bu koleksiyon zaten seçilmiş.", changed: false };
  if (current.length >= MAX_PRODUCT_COLLECTIONS) {
    return { selected: current, message: `En fazla ${MAX_PRODUCT_COLLECTIONS} koleksiyon seçebilirsiniz.`, changed: false };
  }
  return { selected: [...current, trimmed], changed: true };
}

// Burada mevcut koleksiyon checklist'ini ve API'nin ürünle birlikte oluşturacağı yeni adları tek kontrollü seçim alanında yönetiyorum.
export function CollectionSelector({
  collections,
  unavailable = false,
  error,
  onCollectionsChange,
}: {
  collections: Collection[];
  unavailable?: boolean;
  error?: string[];
  onCollectionsChange: () => void;
}) {
  const [selected, setSelected] = useState<string[]>([]);
  const [draft, setDraft] = useState("");
  const [message, setMessage] = useState<string>();
  const labelId = "collections-label";
  const helpId = "collections-help";
  const errorId = "collections-error";

  const applySelection = (name: string, checked: boolean) => {
    const result = updateCollectionSelection(selected, name, checked);
    setMessage(result.message);
    if (!result.changed) return;
    setSelected(result.selected);
    onCollectionsChange();
  };

  // Burada yazılan ad mevcut koleksiyonla eşleşirse API'ye aynı canonical adı gönderiyor, değilse yeni ad olarak seçiyorum.
  const addDraft = () => {
    const matchingCollection = collections.find(
      (collection) => collectionKey(collection.name) === collectionKey(draft),
    );
    const result = updateCollectionSelection(selected, matchingCollection?.name || draft, true);
    setMessage(result.message);
    if (!result.changed) return;
    setSelected(result.selected);
    setDraft("");
    onCollectionsChange();
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== "Enter") return;
    event.preventDefault();
    addDraft();
  };

  return (
    <div>
      <p id={labelId} className="text-sm font-medium text-foreground">Koleksiyonlar</p>
      <div
        role="group"
        aria-labelledby={labelId}
        aria-describedby={`${helpId}${error ? ` ${errorId}` : ""}`}
        className="mt-1.5 max-h-44 overflow-y-auto rounded-lg border border-border-strong bg-surface-strong p-2"
      >
        {collections.length > 0 ? (
          <div className="grid gap-1 sm:grid-cols-2 lg:grid-cols-1">
            {collections.map((collection) => {
              const checked = selected.some((name) => collectionKey(name) === collectionKey(collection.name));
              return (
                <label key={collection.id} className="flex min-h-9 cursor-pointer items-center gap-2 rounded-md px-2 text-sm text-foreground hover:bg-surface-subtle">
                  <input
                    type="checkbox"
                    checked={checked}
                    onChange={(event) => applySelection(collection.name, event.target.checked)}
                    className="size-4 shrink-0 accent-primary"
                  />
                  <span className="min-w-0 break-words">{collection.name}</span>
                </label>
              );
            })}
          </div>
        ) : (
          <p className="px-2 py-1 text-xs leading-5 text-muted">
            {unavailable ? "Mevcut koleksiyonlar yüklenemedi; aşağıdan yeni bir ad ekleyebilirsiniz." : "Henüz kayıtlı koleksiyon yok."}
          </p>
        )}
      </div>

      <label htmlFor="collection-draft" className="mt-2 block text-xs font-semibold text-foreground">
        Yeni koleksiyon adı
      </label>
      <div className="mt-1 flex items-stretch gap-2">
        <input
          id="collection-draft"
          type="text"
          value={draft}
          maxLength={MAX_COLLECTION_NAME_LENGTH}
          onChange={(event) => {
            setDraft(event.target.value);
            setMessage(undefined);
          }}
          onKeyDown={handleKeyDown}
          className="min-h-10 min-w-0 flex-1 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
          aria-invalid={Boolean(error)}
          aria-describedby={`${helpId}${error ? ` ${errorId}` : ""}`}
          placeholder="Örn. Yaz koleksiyonu"
        />
        <button
          type="button"
          onClick={addDraft}
          className="min-h-10 shrink-0 rounded-lg border border-primary/35 bg-primary-soft px-3 text-sm font-semibold text-primary transition-colors hover:border-primary hover:bg-primary-soft/70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
        >
          Koleksiyon ekle
        </button>
      </div>
      <p id={helpId} className="mt-1 text-xs leading-5 text-muted">
        Yeni bir ad eklerseniz ürün kaydedilirken koleksiyon otomatik oluşturulur.
      </p>

      {selected.length > 0 ? (
        <div className="mt-2 flex flex-wrap gap-2" aria-label="Seçilen koleksiyonlar">
          {selected.map((name) => (
            <span key={collectionKey(name)} className="inline-flex min-h-9 max-w-full items-center gap-1 rounded-lg border border-border-strong bg-surface-subtle pl-3 pr-1 text-sm text-foreground">
              <span className="min-w-0 truncate">{name}</span>
              <button
                type="button"
                onClick={() => applySelection(name, false)}
                aria-label={`${name} koleksiyonunu kaldır`}
                className="flex size-8 shrink-0 items-center justify-center rounded-md text-base text-muted transition-colors hover:bg-surface-strong hover:text-danger focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
              >
                ×
              </button>
              <input type="hidden" name="collections" value={name} />
            </span>
          ))}
        </div>
      ) : null}

      {message ? <p className="mt-2 text-xs font-semibold text-warning" role="status">{message}</p> : null}
      {error ? <p id={errorId} className="mt-1 text-xs font-semibold text-danger">{error.join(" ")}</p> : null}
    </div>
  );
}
