"use client";

import { useState, type KeyboardEvent } from "react";

const MAX_TAGS = 20;
const MAX_TAG_LENGTH = 150;

// Burada tag taslağını ve eklenmiş tag listesini formun diğer alanlarından bağımsız, en dar etkileşimli sınırda yönetiyorum.
export function TagEditor({
  initialTags,
  error,
  onTagsChange,
}: {
  initialTags: string[];
  error?: string[];
  onTagsChange: () => void;
}) {
  const [tags, setTags] = useState(() => [...new Set(initialTags.map((tag) => tag.trim()).filter(Boolean))].slice(0, MAX_TAGS));
  const [draft, setDraft] = useState("");
  const [message, setMessage] = useState<string>();
  const helpId = "tags-help";
  const errorId = "tags-error";

  // Burada boş, yinelenen veya sınırı aşan bir tagın listeye eklenmesini önleyip geçerli tagı ayrı bir form alanına dönüştürüyorum.
  const addTag = () => {
    const nextTag = draft.trim();
    if (!nextTag) {
      setMessage("Önce bir tag yazın.");
      return;
    }
    if (tags.length >= MAX_TAGS) {
      setMessage(`En fazla ${MAX_TAGS} tag ekleyebilirsiniz.`);
      return;
    }
    if (tags.some((tag) => tag.localeCompare(nextTag, "tr", { sensitivity: "accent" }) === 0)) {
      setMessage("Bu tag zaten eklenmiş.");
      return;
    }

    setTags((current) => [...current, nextTag]);
    setDraft("");
    setMessage(undefined);
    onTagsChange();
  };

  // Burada Enter tuşunu formu erken göndermek yerine görünür Tag ekle eylemiyle aynı davranışa bağlıyorum.
  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== "Enter") return;
    event.preventDefault();
    addTag();
  };

  // Burada kaldırılan tagı hem görünür listeden hem de gönderilecek gizli form alanlarından çıkarıyorum.
  const removeTag = (tagToRemove: string) => {
    setTags((current) => current.filter((tag) => tag !== tagToRemove));
    setMessage(undefined);
    onTagsChange();
  };

  return (
    <div>
      <label htmlFor="tag-draft" className="block text-sm font-medium text-foreground">
        Etiketler
      </label>
      <div className="mt-1.5 flex items-stretch gap-2">
        <input
          id="tag-draft"
          type="text"
          value={draft}
          maxLength={MAX_TAG_LENGTH}
          onChange={(event) => {
            setDraft(event.target.value);
            setMessage(undefined);
          }}
          onKeyDown={handleKeyDown}
          className="min-h-10 min-w-0 flex-1 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
          aria-invalid={Boolean(error)}
          aria-describedby={`${helpId}${error ? ` ${errorId}` : ""}`}
          placeholder="Örn. Yeni sezon"
        />
        <button
          type="button"
          onClick={addTag}
          className="min-h-10 shrink-0 rounded-lg border border-primary/35 bg-primary-soft px-3 text-sm font-semibold text-primary transition-colors hover:border-primary hover:bg-primary-soft/70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
        >
          Tag ekle
        </button>
      </div>
      <p id={helpId} className="mt-1 text-xs leading-5 text-muted">
        Her tagı ayrı ekleyin. En fazla {MAX_TAGS} tag.
      </p>

      {tags.length > 0 ? (
        <div className="mt-2 flex flex-wrap gap-2" aria-label="Eklenen taglar">
          {tags.map((tag) => (
            <span key={tag} className="inline-flex min-h-9 max-w-full items-center gap-1 rounded-lg border border-border-strong bg-surface-subtle pl-3 pr-1 text-sm text-foreground">
              <span className="min-w-0 truncate">{tag}</span>
              <button
                type="button"
                onClick={() => removeTag(tag)}
                aria-label={`${tag} tagını kaldır`}
                className="flex size-8 shrink-0 items-center justify-center rounded-md text-base text-muted transition-colors hover:bg-surface-strong hover:text-danger focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
              >
                ×
              </button>
              <input type="hidden" name="tags" value={tag} />
            </span>
          ))}
        </div>
      ) : null}

      {message ? <p className="mt-2 text-xs font-semibold text-warning" role="status">{message}</p> : null}
      {error ? <p id={errorId} className="mt-1 text-xs font-semibold text-danger">{error.join(" ")}</p> : null}
    </div>
  );
}
