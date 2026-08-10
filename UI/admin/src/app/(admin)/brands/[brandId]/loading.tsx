// Burada marka düzenleme formu yüklenirken içerik ve görsel rayının nihai geometrisini koruyorum.
export default function EditBrandLoading() {
  return (
    <div className="mx-auto w-full max-w-5xl" aria-busy="true" aria-label="Marka düzenleme formu yükleniyor">
      <div className="mb-5 space-y-2"><div className="h-7 w-40 rounded bg-surface-subtle" /><div className="h-4 w-72 max-w-full rounded bg-surface-subtle" /></div>
      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <div className="h-[30rem] rounded-xl border border-border bg-surface" />
        <div className="space-y-4"><div className="h-64 rounded-xl border border-border bg-surface" /><div className="h-32 rounded-xl border border-border bg-surface" /></div>
      </div>
    </div>
  );
}
