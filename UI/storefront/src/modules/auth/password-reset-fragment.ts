type ResetLocation = Pick<Location, "hash" | "pathname" | "search">;
type ResetHistory = Pick<History, "state" | "replaceState">;

// Burada tek reset tokenını fragmenttan tüketip sorgu dizesine taşımadan adres çubuğunu hemen temizliyorum.
export function consumeResetToken(location: ResetLocation, history: ResetHistory): string | null {
  const fragment = new URLSearchParams(location.hash.slice(1));
  const tokens = fragment.getAll("token");
  const token = tokens.length === 1 ? tokens[0] : null;
  history.replaceState(history.state, "", `${location.pathname}${location.search}`);
  return token && token.trim() ? token : null;
}
