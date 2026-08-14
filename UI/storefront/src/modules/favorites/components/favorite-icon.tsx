// Burada favori kontrollerinin daha ince çizgili, ölçeği bulunduğu yüzeye uyarlanabilen ortak kalp ikonunu paylaşmasını sağlıyorum.
export function FavoriteHeartIcon({
  filled = false,
  className = "size-5",
  strokeWidth = 1.6,
}: {
  filled?: boolean;
  className?: string;
  strokeWidth?: number;
}) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className={className}
      fill={filled ? "currentColor" : "none"}
      stroke="currentColor"
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M20.8 4.7a5.5 5.5 0 0 0-7.8 0L12 5.8l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8l1.1 1.1L12 21l7.8-7.4 1.1-1.1a5.5 5.5 0 0 0-.1-7.8Z" />
    </svg>
  );
}
