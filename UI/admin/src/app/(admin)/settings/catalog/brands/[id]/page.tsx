import { redirect } from "next/navigation";

// Burada eski marka düzenleme bağlantısını aynı kimliği koruyarak canonical rotaya yönlendiriyorum.
export default async function LegacyEditBrandPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  redirect(`/brands/${encodeURIComponent(id)}`);
}
