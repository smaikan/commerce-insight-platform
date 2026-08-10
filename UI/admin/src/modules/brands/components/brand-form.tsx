"use client";

import Link from "next/link";
import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { isTrustedCloudinaryAsset, uploadCloudinaryAsset, validateImageFile, type CloudinaryAsset } from "@/lib/cloudinary/browser-upload";
import { attachBrandImageAction, createBrandAction, updateBrandAction } from "@/modules/brands/actions";
import { BrandImageField, type BrandImageIntent } from "@/modules/brands/components/brand-image-field";
import type { Brand, BrandActionState } from "@/modules/brands/types";
import { initialBrandActionState } from "@/modules/brands/types";

// Burada marka create/edit iş akışını Cloudinary yüklemesi ve API kaydı arasında kısmi başarıyı koruyarak yönetiyorum.
export function BrandForm({ brand }: { brand?: Brand }) {
  const router = useRouter();
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imageIntent, setImageIntent] = useState<BrandImageIntent>("keep");
  const uploadedAssetRef = useRef<CloudinaryAsset | null>(null);

  // Burada client tarafındaki görsel adımını server action sonuçlarıyla tek useActionState akışında birleştiriyorum.
  const submitBrand = async (previousState: BrandActionState, formData: FormData): Promise<BrandActionState> => {
    if (imageFile) {
      const imageError = validateImageFile(imageFile);
      if (imageError) return { status: "error", message: imageError, fieldErrors: { imageUrl: [imageError] } };
    }

    if (brand) {
      formData.set("imageIntent", imageIntent);
      formData.set("imageUrl", imageIntent === "keep" ? brand.imageUrl || "" : "");
      if (imageIntent === "replace" && imageFile) {
        try {
          const folder = `brands/${brand.id}`;
          const asset = await uploadCloudinaryAsset({ file: imageFile, folder, tags: ["brand-image", `brand-${brand.id}`] });
          if (!isTrustedCloudinaryAsset(asset, { folder, allowedResourceTypes: ["image"] })) {
            throw new Error("Görsel yükleme yanıtı doğrulanamadı.");
          }
          formData.set("imageUrl", asset.secureUrl);
          formData.set("imagePublicId", asset.publicId);
          formData.set("imageResourceType", asset.resourceType);
        } catch (error) {
          return { status: "error", message: error instanceof Error ? error.message : "Görsel yüklenemedi." };
        }
      }
      return updateBrandAction(brand.id, previousState, formData);
    }

    const created = previousState.brandId
      ? { status: "created" as const, brandId: previousState.brandId, message: previousState.message }
      : await createBrandAction(previousState, formData);
    if (created.status !== "created" || !created.brandId) return created;
    if (!imageFile) return { status: "success", redirectHref: "/brands?created=1", message: "Marka oluşturuldu." };

    try {
      const folder = `brands/${created.brandId}`;
      const asset = uploadedAssetRef.current || await uploadCloudinaryAsset({ file: imageFile, folder, tags: ["brand-image", `brand-${created.brandId}`] });
      if (!isTrustedCloudinaryAsset(asset, { folder, allowedResourceTypes: ["image"] })) {
        throw new Error("Görsel yükleme yanıtı doğrulanamadı.");
      }
      uploadedAssetRef.current = asset;
      const attached = await attachBrandImageAction(created.brandId, asset);
      if (attached.status === "success") return attached;
      return {
        status: "partial",
        brandId: created.brandId,
        message: attached.message || "Marka oluşturuldu ancak görsel bağlanamadı.",
      };
    } catch (error) {
      return {
        status: "partial",
        brandId: created.brandId,
        message: error instanceof Error ? `Marka oluşturuldu ancak ${error.message.toLocaleLowerCase("tr-TR")}` : "Marka oluşturuldu ancak görsel yüklenemedi.",
      };
    }
  };

  const [state, formAction, pending] = useActionState(submitBrand, initialBrandActionState);

  // Burada kalıcı başarı veya kısmi başarı sonucunda kullanıcının doğru yetkili kaydı yeniden okumasını sağlıyorum.
  useEffect(() => {
    if (!state.redirectHref) return;
    router.replace(state.redirectHref);
    router.refresh();
  }, [router, state.redirectHref]);

  return (
    <form action={formAction} className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_18rem]">
      <section className="rounded-xl border border-border bg-surface-strong p-4 sm:p-5" aria-labelledby="brand-details-title">
        <div className="border-b border-border pb-4">
          <h2 id="brand-details-title" className="text-base font-semibold text-foreground">Marka bilgileri</h2>
          <p className="mt-1 text-sm leading-5 text-muted">Ürünlerde ve mağaza yüzeylerinde kullanılacak marka kimliğini belirleyin.</p>
        </div>
        <div className="mt-5 grid gap-4">
          <Field label="Marka adı" name="name" defaultValue={brand?.name || ""} required maxLength={150} error={fieldError(state, "name")} />
          <Field label="URL değeri" name="url" defaultValue={brand?.url || ""} maxLength={200} error={fieldError(state, "url")} help="Boş bırakırsanız backend uygun bağlantı değerini üretebilir." />
          <label className="block text-sm font-medium text-foreground">
            Açıklama
            <textarea name="description" rows={6} maxLength={1000} defaultValue={brand?.description || ""} aria-invalid={Boolean(fieldError(state, "description"))} className="mt-1.5 w-full rounded-lg border border-border-strong bg-surface-strong px-3 py-2 text-sm leading-6 text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft" />
            {fieldError(state, "description") ? <span className="mt-1 block text-xs font-semibold text-danger">{fieldError(state, "description")}</span> : <span className="mt-1 block text-xs font-normal leading-5 text-muted">En fazla 1.000 karakter.</span>}
          </label>
        </div>
      </section>

      <aside className="space-y-4 lg:sticky lg:top-20">
        <BrandImageField
          existingImageUrl={brand?.imageUrl}
          disabled={pending}
          file={imageFile}
          intent={imageIntent}
          error={fieldError(state, "imageUrl")}
          onChange={(file, intent) => { uploadedAssetRef.current = null; setImageFile(file); setImageIntent(intent); }}
        />
        {!brand ? (
          <section className="rounded-xl border border-border bg-surface p-4">
            <h2 className="text-sm font-semibold text-foreground">Kullanılabilirlik</h2>
            <label className="mt-3 flex items-start gap-3 rounded-lg border border-border bg-surface-subtle/50 p-3">
              <input name="isActive" type="checkbox" defaultChecked className="mt-0.5 size-4 rounded border-border-strong text-primary focus:ring-focus" />
              <span><span className="block text-sm font-semibold text-foreground">Aktif oluştur</span><span className="mt-0.5 block text-xs leading-5 text-muted">Marka ürün formlarında hemen kullanılabilir.</span></span>
            </label>
          </section>
        ) : (
          <section className="rounded-xl border border-border bg-surface p-4">
            <h2 className="text-sm font-semibold text-foreground">Mevcut durum</h2>
            <p className="mt-2 text-sm leading-5 text-muted">Marka şu anda <strong className="text-foreground">{brand.isActive ? "aktif" : "pasif"}</strong>. Aktiflik liste ekranından değiştirilir.</p>
          </section>
        )}
      </aside>

      {state.status === "error" || state.status === "partial" ? (
        <div role="alert" className="rounded-xl border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900 lg:col-span-2">
          <p className="font-semibold">{state.message}</p>
          {state.traceId ? <p className="mt-1 text-xs">Takip kodu: {state.traceId}</p> : null}
        </div>
      ) : null}

      <div className="flex flex-col-reverse gap-2 border-t border-border pt-4 sm:flex-row sm:justify-end lg:col-span-2">
        <Link href="/brands" className="inline-flex min-h-11 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle">Vazgeç</Link>
        <button type="submit" disabled={pending} className="inline-flex min-h-11 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">
          {pending ? imageFile ? "Görsel ve marka kaydediliyor…" : "Kaydediliyor…" : brand ? "Değişiklikleri kaydet" : state.status === "partial" ? "Görseli yeniden dene" : "Marka oluştur"}
        </button>
      </div>
    </form>
  );
}

// Burada tek satırlı alanları kalıcı etiket, yardım ve doğrulama mesajıyla tutarlı çiziyorum.
function Field({ label, name, defaultValue, required = false, maxLength, help, error }: { label: string; name: string; defaultValue: string; required?: boolean; maxLength?: number; help?: string; error?: string }) {
  return (
    <label className="block text-sm font-medium text-foreground">
      {label}{required ? " *" : ""}
      <input name={name} defaultValue={defaultValue} required={required} maxLength={maxLength} aria-invalid={Boolean(error)} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft" />
      {error ? <span className="mt-1 block text-xs font-semibold text-danger">{error}</span> : help ? <span className="mt-1 block text-xs font-normal leading-5 text-muted">{help}</span> : null}
    </label>
  );
}

// Burada backend alan hatalarının ilk güvenli mesajını ilgili kontrole bağlıyorum.
function fieldError(state: BrandActionState, field: string): string | undefined {
  return state.fieldErrors?.[field]?.[0];
}
