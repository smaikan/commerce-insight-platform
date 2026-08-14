type ResetNavigation = Pick<Location, "replace">;

// Burada parola değişiminden sonra eski istemci belleğini de bırakan tam belge login yönlendirmesini başlatıyorum.
export function redirectAfterPasswordReset(location: ResetNavigation): void {
  location.replace("/login?passwordReset=1");
}
