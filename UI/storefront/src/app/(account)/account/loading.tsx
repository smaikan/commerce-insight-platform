// Burada hesap verileri yüklenirken son içeriğin geometrisini koruyan sakin bir iskelet gösteriyorum.
export default function AccountLoading() {
  return (
    <div aria-label="Hesap bilgileri yükleniyor" aria-busy="true">
      <div className="h-24 border-b border-line bg-surface-subtle" />
      <div className="mt-7 grid gap-6 xl:grid-cols-2">
        <div className="h-72 border border-line bg-surface" />
        <div className="h-72 border border-line bg-surface" />
      </div>
    </div>
  );
}
