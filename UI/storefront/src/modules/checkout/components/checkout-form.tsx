"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  useEffect,
  useRef,
  useState,
  type InputHTMLAttributes,
} from "react";

import {
  checkoutFieldErrors,
  checkoutProblemMessage,
  checkoutTraceId,
  initializeIyzicoCheckoutForm,
  isCartConflict,
  isCheckoutChallengeRequired,
  paymentIntentKey,
  redirectToPaymentPage,
  submitGuestCheckout,
  previewCoupon,
} from "@/modules/checkout/client/checkout-api";
import { createMemberOrderAction } from "@/modules/checkout/actions";
import { TurnstileChallenge } from "@/modules/checkout/components/turnstile-challenge";
import type {
  GuestAddressRequest,
  GuestCheckoutRequest,
  CheckoutAddress,
  MemberCheckoutRequest,
  ShippingMethod,
} from "@/modules/checkout/types";
import {
  loadCart,
  subscribeToCart,
} from "@/modules/cart/client/cart-api";
import type { Cart } from "@/modules/cart/types";
import { TurkiyeAddressFields } from "@/components/storefront/turkiye-address-fields";
import { PhoneField } from "@/components/storefront/phone-field";

type CartState =
  | { kind: "loading" }
  | { kind: "ready"; cart: Cart }
  | { kind: "error"; message: string };

type Intent = {
  serializedBody: string;
  idempotencyKey: string;
};

type FieldErrors = Record<string, string>;

// Burada checkout taslağını, cart snapshot'ını ve tek intent submit durumunu en yakın form sınırında tutuyorum.
export function CheckoutForm({
  shippingMethods,
  currency,
  turnstileSiteKey,
  orderCreationEnabled,
  accountAddresses,
}: {
  shippingMethods: ShippingMethod[];
  currency: string;
  turnstileSiteKey: string;
  orderCreationEnabled: boolean;
  accountAddresses: CheckoutAddress[] | null;
}) {
  const router = useRouter();
  // Burada SSR ve hydration'ın ilk görünümünü deterministik tutup paylaşılan client snapshot'ını effect sonrasında tüketiyorum.
  const [cartState, setCartState] = useState<CartState>({ kind: "loading" });
  const [sameBillingAddress, setSameBillingAddress] = useState(true);
  const [selectedShippingMethodId, setSelectedShippingMethodId] = useState(shippingMethods[0]?.id || "");
  const memberShippingAddresses = accountAddresses?.filter((address) => address.type === 0) || [];
  const [selectedAddressId, setSelectedAddressId] = useState(memberShippingAddresses.find((address) => address.isDefault)?.id || memberShippingAddresses[0]?.id || "");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<{ message: string; traceId?: string } | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [challengeRequired, setChallengeRequired] = useState(false);
  const [turnstileToken, setTurnstileToken] = useState("");
  const [challengeError, setChallengeError] = useState<string>();
  const [challengeResetVersion, setChallengeResetVersion] = useState(0);
  const [errorFocusVersion, setErrorFocusVersion] = useState(0);
  const errorSummaryRef = useRef<HTMLDivElement>(null);
  const intentRef = useRef<Intent | null>(null);
  const submittingRef = useRef(false);
  const [couponCode, setCouponCode] = useState("");
  const [couponPreview, setCouponPreview] = useState<{ discountTotal: number; code: string } | null>(null);
  const [isApplyingCoupon, setIsApplyingCoupon] = useState(false);
  const [couponError, setCouponError] = useState<string>();
  const isMember = accountAddresses !== null;

  useEffect(() => {
    const unsubscribe = subscribeToCart((cart) => setCartState({ kind: "ready", cart }));
    void loadCart()
      .then((cart) => setCartState({ kind: "ready", cart }))
      .catch(() => setCartState({ kind: "error", message: "Sepetiniz yüklenemedi. Lütfen tekrar deneyin." }));
    return unsubscribe;
  }, []);

  useEffect(() => {
    if (errorFocusVersion > 0) errorSummaryRef.current?.focus();
  }, [errorFocusVersion]);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submittingRef.current || cartState.kind !== "ready") return;

    if (!orderCreationEnabled) {
      setSubmitError({ message: "Online sipariş şu anda kullanılamıyor. Ödeme seçeneği etkinleştirildiğinde tekrar deneyebilirsiniz." });
      setErrorFocusVersion((version) => version + 1);
      return;
    }

    const displayedCart = cartState.cart;
    const formData = new FormData(event.currentTarget);
    const draftResult = isMember
      ? memberCheckoutRequestFromForm(formData, displayedCart)
      : checkoutRequestFromForm(formData, sameBillingAddress, displayedCart);
    if (!draftResult.value) {
      setFieldErrors(draftResult.errors);
      setSubmitError(null);
      setErrorFocusVersion((version) => version + 1);
      return;
    }

    if (!displayedCart.concurrencyToken || displayedCart.items.length === 0 || displayedCart.hasUnavailableItems || displayedCart.hasPriceChanges) {
      setFieldErrors({});
      setSubmitError({ message: "Siparişi tamamlamadan önce sepetinizin son durumunu kontrol edin." });
      setErrorFocusVersion((version) => version + 1);
      return;
    }

    if (!isMember && challengeRequired && !turnstileToken) {
      setSubmitError({ message: "Siparişi oluşturmadan önce güvenlik doğrulamasını tamamlayın." });
      setErrorFocusVersion((version) => version + 1);
      return;
    }

    setFieldErrors({});
    setSubmitError(null);
    submittingRef.current = true;
    setIsSubmitting(true);

    let createdOrderId: string | null = null;

    try {
      // Burada sipariş intent'ini oluşturmadan hemen önce sepeti API'den zorla yenileyip eski concurrency token ile checkout yapılmasını engelliyorum.
      const freshCart = await loadCart(true);
      if (freshCart.concurrencyToken !== displayedCart.concurrencyToken) {
        setSubmitError({ message: "Sepetiniz başka bir işlemde güncellendi. Sipariş özetinin son halini kontrol edip tekrar deneyin." });
        setErrorFocusVersion((version) => version + 1);
        return;
      }

      const result = isMember
        ? memberCheckoutRequestFromForm(formData, freshCart)
        : checkoutRequestFromForm(formData, sameBillingAddress, freshCart);
      if (!result.value || !freshCart.concurrencyToken || freshCart.items.length === 0 || freshCart.hasUnavailableItems || freshCart.hasPriceChanges) {
        setFieldErrors(result.errors);
        setSubmitError({ message: "Siparişi tamamlamadan önce sepetinizin son durumunu kontrol edin." });
        setErrorFocusVersion((version) => version + 1);
        return;
      }

      let order;
      if (isMember) {
        const memberResult = await createMemberOrderAction(result.value);
        if (!memberResult.ok) throw memberResult.problem;
        order = memberResult.order;
      } else {
        const serializedBody = JSON.stringify(result.value);
        if (!intentRef.current || intentRef.current.serializedBody !== serializedBody) {
          intentRef.current = { serializedBody, idempotencyKey: crypto.randomUUID() };
        }
        order = await submitGuestCheckout(result.value as GuestCheckoutRequest, intentRef.current.idempotencyKey, turnstileToken || undefined);
      }
      createdOrderId = order.id;
      if (order.status === 2 || order.grandTotal === 0) {
        router.push(`/checkout/confirmation/${encodeURIComponent(order.id)}`);
        return;
      }

      const session = await initializeIyzicoCheckoutForm(order.id, paymentIntentKey(order.id));
      redirectToPaymentPage(session);
    } catch (error) {
      if (createdOrderId) {
        router.push(`/checkout/confirmation/${encodeURIComponent(createdOrderId)}?payment=retry`);
        return;
      }
      if (isCheckoutChallengeRequired(error)) {
        const challengeWasAttempted = Boolean(turnstileToken);
        setChallengeRequired(true);
        setTurnstileToken("");
        setChallengeError(challengeWasAttempted ? "Doğrulamanın süresi doldu veya doğrulama kabul edilmedi. Lütfen yeniden deneyin." : undefined);
        if (challengeWasAttempted) setChallengeResetVersion((version) => version + 1);
      }

      if (isCartConflict(error)) {
        await loadCart(true).catch(() => undefined);
      }

      const apiFieldErrors = mapApiFieldErrors(checkoutFieldErrors(error));
      setFieldErrors(apiFieldErrors);
      setSubmitError({ message: checkoutProblemMessage(error), traceId: checkoutTraceId(error) });
      setErrorFocusVersion((version) => version + 1);
    } finally {
      submittingRef.current = false;
      setIsSubmitting(false);
    }
  }

  if (cartState.kind === "loading") return <CheckoutLoadingState />;

  if (cartState.kind === "error") {
    return (
      <CheckoutMessage title="Sepet yüklenemedi" message={cartState.message} href="/cart" action="Sepete dön" />
    );
  }

  const cart = cartState.cart;
  if (cart.items.length === 0) {
    return (
      <CheckoutMessage title="Siparişi tamamlamak için sepetiniz boş" message="Önce alışverişinize ürün ekleyin." href="/products" action="Ürünleri keşfet" />
    );
  }

  const checkoutBlocked = !orderCreationEnabled || cart.hasUnavailableItems || cart.hasPriceChanges || shippingMethods.length === 0 || (isMember && memberShippingAddresses.length === 0);
  const selectedShippingMethod = shippingMethods.find((method) => method.id === selectedShippingMethodId);

  async function handleApplyCoupon() {
    if (!couponCode) return;
    setIsApplyingCoupon(true);
    setCouponError(undefined);
    try {
      const result = await previewCoupon(couponCode);
      setCouponPreview(result);
      setFieldErrors((prev) => {
        const next = { ...prev };
        delete next.couponCode;
        return next;
      });
    } catch (error) {
      setCouponPreview(null);
      setCouponError(checkoutProblemMessage(error));
    } finally {
      setIsApplyingCoupon(false);
    }
  }

  return (
    <main id="main-content" className="page-shell max-w-[80rem] flex-1 py-8 sm:py-12 lg:py-14">
      <header className="max-w-2xl border-b border-line pb-6 sm:pb-8">
        <p className="mb-2 text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">Güvenli sipariş</p>
        <h1 className="text-3xl font-semibold tracking-[-0.04em] text-ink sm:text-4xl">Teslimat bilgileri</h1>
        <p className="mt-3 text-sm leading-6 text-ink-muted">Sipariş ve teslimat güncellemeleri için doğru iletişim bilgilerini kullanın.</p>
      </header>

      {!orderCreationEnabled ? (
        <p className="mt-6 max-w-2xl rounded-xl border border-brand-700/25 bg-surface-subtle px-4 py-3 text-sm leading-6 text-ink" role="status">
          Online sipariş şu anda kullanılamıyor. Ödeme seçeneği etkinleştirildiğinde bu adımı tamamlayabilirsiniz.
        </p>
      ) : null}

      <form className="mt-7 grid items-start gap-8 lg:grid-cols-[minmax(0,1fr)_minmax(18rem,23rem)] lg:gap-10" noValidate onSubmit={handleSubmit}>
        <div className="space-y-6">
          {(Object.keys(fieldErrors).length > 0 || submitError) ? (
            <div ref={errorSummaryRef} tabIndex={-1} className="focus-ring rounded-xl border border-danger/30 bg-danger/5 px-4 py-4" role="alert" aria-labelledby="checkout-error-title">
              <h2 id="checkout-error-title" className="text-sm font-bold text-danger">Sipariş tamamlanamadı</h2>
              {submitError ? <p className="mt-1 text-sm leading-5 text-danger">{submitError.message}</p> : null}
              {Object.keys(fieldErrors).length > 0 ? (
                <ul className="mt-2 list-disc space-y-1 pl-5 text-sm text-danger">
                  {Object.entries(fieldErrors).map(([name, message]) => (
                    <li key={name}><a className="underline underline-offset-2" href={`#${name}`}>{message}</a></li>
                  ))}
                </ul>
              ) : null}
              {submitError?.traceId ? <p className="mt-2 text-xs text-ink-muted">Referans: {submitError.traceId}</p> : null}
            </div>
          ) : null}

          <div className="divide-y divide-line overflow-hidden rounded-2xl border border-line bg-surface">
            {isMember ? (
              <CheckoutSection title="Teslimat adresi" description="Hesabınızdaki teslimat adreslerinden birini seçin.">
                {memberShippingAddresses.length > 0 ? (
                  <fieldset>
                    <legend className="sr-only">Teslimat adresi seçin</legend>
                    <div className="grid gap-3">
                      {memberShippingAddresses.map((address) => (
                        <label key={address.id} className={`flex cursor-pointer items-start gap-3 rounded-xl border p-4 ${selectedAddressId === address.id ? "border-brand-700 bg-surface-subtle" : "border-line bg-surface"}`}>
                          <input
                            type="radio"
                            name="shippingAddressId"
                            value={address.id}
                            checked={selectedAddressId === address.id}
                            onChange={() => setSelectedAddressId(address.id)}
                            className="mt-1 size-4 shrink-0 accent-brand-700"
                          />
                          <span className="min-w-0 text-sm leading-6 text-ink-muted">
                            <span className="block font-bold text-ink">{address.title}{address.isDefault ? " · Varsayılan" : ""}</span>
                            <span className="block">{address.firstName} {address.lastName}</span>
                            <span className="block">{address.fullAddress}</span>
                            <span className="block">{address.district} / {address.city}</span>
                          </span>
                        </label>
                      ))}
                    </div>
                    {fieldErrors.shippingAddressId ? <p className="mt-2 text-sm font-semibold text-danger">{fieldErrors.shippingAddressId}</p> : null}
                  </fieldset>
                ) : (
                  <div className="rounded-xl border border-danger/25 bg-danger/5 p-4 text-sm leading-6 text-danger">
                    <p>Teslimat için hesabınıza bir teslimat adresi eklemeniz gerekiyor.</p>
                    <Link href="/account/addresses" className="focus-ring mt-3 inline-flex min-h-11 items-center font-bold underline underline-offset-4">Adreslerime git</Link>
                  </div>
                )}
              </CheckoutSection>
            ) : (
              <>
                <CheckoutSection title="İletişim" description="Sipariş durumu ve erişim bağlantısı bu e-posta adresine gönderilir.">
                  <div className="grid gap-4 sm:grid-cols-2">
                    <TextField name="customerEmail" label="E-posta" type="email" inputMode="email" autoComplete="email" maxLength={320} required error={fieldErrors.customerEmail} />
                    <PhoneField name="customerPhoneNumber" label="Telefon" autoComplete="tel" required error={fieldErrors.customerPhoneNumber} />
                  </div>
                </CheckoutSection>

                <CheckoutSection title="Teslimat adresi" description="Teslimatı alacak kişinin ve adresin bilgilerini girin.">
                  <AddressFields prefix="shipping" errors={fieldErrors} />
                </CheckoutSection>

                <CheckoutSection title="Fatura adresi">
                  <label className="flex min-h-11 cursor-pointer items-center gap-3 text-sm font-semibold text-ink">
                    <input type="checkbox" checked={sameBillingAddress} onChange={(event) => setSameBillingAddress(event.target.checked)} className="size-4 accent-brand-700" />
                    Fatura adresim teslimat adresimle aynı
                  </label>
                  {!sameBillingAddress ? <div className="mt-5"><AddressFields prefix="billing" errors={fieldErrors} /></div> : null}
                </CheckoutSection>
              </>
            )}

            <CheckoutSection title="Kargo yöntemi" description="Kargo adı ve ücreti sipariş oluşturulurken API tarafından yeniden doğrulanır.">
              {shippingMethods.length > 0 ? (
                <fieldset>
                  <legend className="sr-only">Kargo yöntemi seçin</legend>
                  <div className="grid gap-3">
                    {shippingMethods.map((method) => (
                      <label key={method.id} className={`flex min-h-16 cursor-pointer items-center gap-3 rounded-xl border p-4 ${selectedShippingMethodId === method.id ? "border-brand-700 bg-surface-subtle" : "border-line bg-surface"}`}>
                        <input
                          type="radio"
                          name="shippingMethodId"
                          value={method.id}
                          checked={selectedShippingMethodId === method.id}
                          onChange={() => setSelectedShippingMethodId(method.id)}
                          className="size-4 accent-brand-700"
                        />
                        <span className="min-w-0 flex-1 text-sm font-bold text-ink">{method.name}</span>
                        <span className="shrink-0 text-sm font-semibold text-ink">{method.fixedFee === 0 ? "Ücretsiz" : formatMoney(method.fixedFee, currency)}</span>
                      </label>
                    ))}
                  </div>
                  {fieldErrors.shippingMethodId ? <p className="mt-2 text-sm font-semibold text-danger">{fieldErrors.shippingMethodId}</p> : null}
                </fieldset>
              ) : (
                <p className="rounded-lg bg-danger/5 px-3 py-3 text-sm font-semibold text-danger">Şu anda kullanılabilir bir kargo yöntemi bulunmuyor.</p>
              )}
            </CheckoutSection>

            <CheckoutSection title="Kupon" description="Kupon opsiyoneldir ve uygunluğu sipariş oluşturulurken kontrol edilir.">
              <div className="flex items-start gap-3">
                <div className="flex-1">
                  <TextField 
                    name="couponCode" 
                    label="Kupon kodu (opsiyonel)" 
                    autoComplete="off" 
                    maxLength={50} 
                    value={couponCode}
                    onChange={(e) => setCouponCode(e.target.value)}
                    error={couponError || fieldErrors.couponCode} 
                  />
                </div>
                <button 
                  type="button" 
                  onClick={handleApplyCoupon} 
                  disabled={!couponCode || isApplyingCoupon}
                  className="mt-7 shrink-0 rounded-xl bg-surface px-5 py-3 text-sm font-semibold text-ink shadow-sm ring-1 ring-inset ring-line hover:bg-surface-hover disabled:opacity-50"
                >
                  {isApplyingCoupon ? "Hesaplanıyor..." : "Uygula"}
                </button>
              </div>
            </CheckoutSection>
          </div>
        </div>

        <aside className="rounded-2xl border border-line bg-surface p-5 shadow-panel sm:p-6 lg:sticky lg:top-28" aria-labelledby="checkout-summary-title">
          <h2 id="checkout-summary-title" className="text-lg font-bold text-ink">Sipariş özeti</h2>
          <ul className="mt-5 divide-y divide-line border-y border-line">
            {cart.items.map((item) => (
              <li key={item.id} className="flex items-start justify-between gap-4 py-3 text-sm">
                <span className="min-w-0 text-ink-muted"><span className="font-semibold text-ink">{item.productTitle || "Ürün"}</span><span className="ml-1">× {item.quantity}</span></span>
                <span className="shrink-0 font-semibold tabular-nums text-ink">{formatMoney(item.totalPrice, currency)}</span>
              </li>
            ))}
          </ul>
          <dl className="mt-5 space-y-3 text-sm">
            <div className="flex justify-between gap-4 text-ink-muted"><dt>Ara toplam</dt><dd className="font-semibold tabular-nums text-ink">{formatMoney(cart.subTotal, currency)}</dd></div>
            {couponPreview ? <div className="flex justify-between gap-4 text-success"><dt>İndirim ({couponPreview.code})</dt><dd className="font-semibold tabular-nums">- {formatMoney(couponPreview.discountTotal, currency)}</dd></div> : null}
            <div className="flex justify-between gap-4 text-ink-muted"><dt>Kargo</dt><dd className="font-semibold tabular-nums text-ink">{selectedShippingMethod ? (selectedShippingMethod.fixedFee === 0 ? "Ücretsiz" : formatMoney(selectedShippingMethod.fixedFee, currency)) : "Seçilmedi"}</dd></div>
            <div className="border-t border-line pt-3"><dt className="font-semibold text-ink">Son toplam</dt><dd className="mt-1 font-semibold tabular-nums text-brand-700">{formatMoney(Math.max(0, cart.subTotal - (couponPreview?.discountTotal || 0)) + (selectedShippingMethod?.fixedFee || 0), currency)}</dd></div>
          </dl>

          {cart.hasUnavailableItems ? <p className="mt-5 rounded-lg bg-danger/5 px-3 py-3 text-sm text-danger">Kullanılamayan ürünleri sepetten kaldırın.</p> : null}
          {cart.hasPriceChanges ? <p className="mt-3 rounded-lg bg-surface-subtle px-3 py-3 text-sm text-ink">Değişen fiyatları sepette kabul edin.</p> : null}

          {!isMember && challengeRequired ? (
            <TurnstileChallenge
              siteKey={turnstileSiteKey}
              resetVersion={challengeResetVersion}
              error={challengeError}
              onToken={(token) => {
                setTurnstileToken(token);
                setChallengeError(undefined);
                setSubmitError(null);
              }}
              onExpired={() => {
                setTurnstileToken("");
                setChallengeError("Doğrulamanın süresi doldu. Lütfen tekrar tamamlayın.");
              }}
              onError={() => {
                setTurnstileToken("");
                setChallengeError("Güvenlik doğrulaması yüklenemedi. Bağlantınızı kontrol edip tekrar deneyin.");
              }}
            />
          ) : null}

          <button type="submit" disabled={checkoutBlocked || isSubmitting || (!isMember && challengeRequired && !turnstileToken)} aria-busy={isSubmitting} className="focus-ring mt-6 min-h-12 w-full rounded-lg bg-brand-700 px-5 text-sm font-bold text-white hover:bg-brand-950 disabled:cursor-not-allowed disabled:bg-line disabled:text-ink-muted">
            {isSubmitting ? "Güvenli ödeme hazırlanıyor…" : "Güvenli ödemeye geç"}
          </button>
          {!orderCreationEnabled ? <p className="mt-2 text-xs leading-5 text-ink-muted">Online sipariş verme geçici olarak kapalıdır.</p> : null}
          {orderCreationEnabled && checkoutBlocked ? <p className="mt-2 text-xs leading-5 text-ink-muted">Devam etmek için sepet ve kargo uyarılarını çözün.</p> : null}
          {!isMember && challengeRequired && !turnstileToken ? <p className="mt-2 text-xs leading-5 text-ink-muted">Güvenlik doğrulaması tamamlandığında sipariş butonu açılır.</p> : null}
          <Link href="/cart" className="focus-ring mt-3 inline-flex min-h-11 w-full items-center justify-center text-sm font-bold text-brand-700 hover:text-brand-950">Sepete dön</Link>
          <p className="mt-4 border-t border-line pt-4 text-xs leading-5 text-ink-muted">Kart bilgileriniz iyzico’nun güvenli ödeme sayfasında alınır; mağaza bu bilgileri toplamaz veya saklamaz.</p>
        </aside>
      </form>
    </main>
  );
}

// Burada form bölümlerini ortak yüzey ve başlık hiyerarşisiyle düzenliyorum.
function CheckoutSection({ title, description, children }: { title: string; description?: string; children: React.ReactNode }) {
  return (
    <section className="p-5 sm:p-6">
      <h2 className="text-lg font-bold text-ink">{title}</h2>
      {description ? <p className="mt-1 text-sm leading-5 text-ink-muted">{description}</p> : null}
      <div className="mt-5">{children}</div>
    </section>
  );
}

// Burada tekrar eden input etiket, zorunluluk ve hata ilişkilendirmesini erişilebilir tek kontrolde tutuyorum.
function TextField({ name, label, error, className = "", ...props }: InputHTMLAttributes<HTMLInputElement> & { name: string; label: string; error?: string }) {
  const errorId = `${name}-error`;
  return (
    <label htmlFor={name} className={`block ${className}`}>
      <span className="mb-2 block text-sm font-semibold text-ink">{label}{props.required ? <span className="ml-1 text-danger" aria-hidden="true">*</span> : null}</span>
      <input
        {...props}
        id={name}
        name={name}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        className="focus-ring min-h-12 w-full rounded-lg border border-line bg-surface px-3 text-sm text-ink placeholder:text-ink-muted/70 aria-[invalid=true]:border-danger"
      />
      {error ? <span id={errorId} className="mt-1.5 block text-sm font-semibold text-danger">{error}</span> : null}
    </label>
  );
}

// Burada shipping ve opsiyonel billing adresinin aynı alan sözleşmesini tekrar eden markup üretmeden sunuyorum.
function AddressFields({ prefix, errors }: { prefix: "shipping" | "billing"; errors: FieldErrors }) {
  const autocompletePrefix = prefix === "shipping" ? "shipping" : "billing";
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <TextField name={`${prefix}Title`} label="Adres başlığı" autoComplete="off" maxLength={100} required error={errors[`${prefix}Title`]} className="sm:col-span-2" />
      <TextField name={`${prefix}FirstName`} label="Alıcı adı" autoComplete={`${autocompletePrefix} given-name`} maxLength={100} required error={errors[`${prefix}FirstName`]} />
      <TextField name={`${prefix}LastName`} label="Alıcı soyadı" autoComplete={`${autocompletePrefix} family-name`} maxLength={100} required error={errors[`${prefix}LastName`]} />
      <TurkiyeAddressFields prefix={prefix} errors={errors} variant="checkout" />
      <label htmlFor={`${prefix}FullAddress`} className="block sm:col-span-2">
        <span className="mb-2 block text-sm font-semibold text-ink">Açık adres<span className="ml-1 text-danger" aria-hidden="true">*</span></span>
        <textarea
          id={`${prefix}FullAddress`}
          name={`${prefix}FullAddress`}
          autoComplete={`${autocompletePrefix} street-address`}
          maxLength={500}
          required
          rows={4}
          aria-invalid={Boolean(errors[`${prefix}FullAddress`])}
          aria-describedby={errors[`${prefix}FullAddress`] ? `${prefix}FullAddress-error` : undefined}
          className="focus-ring w-full resize-y rounded-lg border border-line bg-surface px-3 py-3 text-sm text-ink aria-[invalid=true]:border-danger"
        />
        {errors[`${prefix}FullAddress`] ? <span id={`${prefix}FullAddress-error`} className="mt-1.5 block text-sm font-semibold text-danger">{errors[`${prefix}FullAddress`]}</span> : null}
      </label>
      <TextField name={`${prefix}PostalCode`} label="Posta kodu (opsiyonel)" inputMode="numeric" autoComplete={`${autocompletePrefix} postal-code`} maxLength={20} error={errors[`${prefix}PostalCode`]} className="sm:col-span-2 sm:max-w-xs" />
    </div>
  );
}

export function CheckoutLoadingState() {
  return (
    <main id="main-content" className="page-shell max-w-[80rem] flex-1 py-8 sm:py-12 lg:py-14" aria-label="Sipariş sayfası yükleniyor" aria-busy="true">
      <div className="h-10 w-64 rounded bg-line/70" />
      <div className="mt-10 grid gap-8 lg:grid-cols-[minmax(0,1fr)_minmax(18rem,23rem)]">
        <div className="h-[32rem] rounded-2xl border border-line bg-surface" />
        <div className="h-72 rounded-2xl border border-line bg-surface" />
      </div>
    </main>
  );
}

function CheckoutMessage({ title, message, href, action }: { title: string; message: string; href: string; action: string }) {
  return (
    <main id="main-content" className="page-shell flex flex-1 items-center justify-center py-16 sm:py-24">
      <section className="w-full max-w-xl rounded-2xl border border-line bg-surface px-6 py-10 text-center shadow-panel sm:px-10">
        <h1 className="text-2xl font-semibold tracking-[-0.03em] text-ink">{title}</h1>
        <p className="mt-3 text-sm leading-6 text-ink-muted">{message}</p>
        <Link href={href} className="focus-ring mt-6 inline-flex min-h-12 items-center justify-center rounded-lg bg-brand-700 px-6 text-sm font-bold text-white hover:bg-brand-950">{action}</Link>
      </section>
    </main>
  );
}

function checkoutRequestFromForm(form: FormData, sameBillingAddress: boolean, cart: Cart): { value: GuestCheckoutRequest | null; errors: FieldErrors } {
  const errors: FieldErrors = {};
  const customerEmail = requiredFormValue(form, "customerEmail", "E-posta", 320, errors);
  const customerPhoneNumber = requiredFormValue(form, "customerPhoneNumber", "Telefon", 30, errors);
  if (customerEmail && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(customerEmail)) errors.customerEmail = "Geçerli bir e-posta adresi girin.";

  const shippingAddress = addressFromForm(form, "shipping", errors, customerPhoneNumber);
  const billingAddress = sameBillingAddress ? undefined : addressFromForm(form, "billing", errors, customerPhoneNumber);
  const customerFirstName = shippingAddress?.firstName || "";
  const customerLastName = shippingAddress?.lastName || "";
  const shippingMethodId = formValue(form, "shippingMethodId");
  if (!shippingMethodId) errors.shippingMethodId = "Bir kargo yöntemi seçin.";
  const couponCode = formValue(form, "couponCode");
  if (couponCode.length > 50) errors.couponCode = "Kupon kodu en fazla 50 karakter olabilir.";

  if (Object.keys(errors).length || !cart.concurrencyToken || !shippingMethodId || !shippingAddress || (!sameBillingAddress && !billingAddress)) {
    return { value: null, errors };
  }

  return {
    value: {
      expectedCartConcurrencyToken: cart.concurrencyToken,
      customer: { firstName: customerFirstName, lastName: customerLastName, email: customerEmail, phoneNumber: customerPhoneNumber },
      shippingAddress,
      ...(billingAddress ? { billingAddress } : {}),
      shippingMethodId,
      ...(couponCode ? { couponCode } : {}),
    },
    errors,
  };
}

// Burada üye checkout isteğini yalnız hesap adresi, kargo ve güncel sepet token'ından oluşturarak guest kişisel alanlarını göndermiyorum.
function memberCheckoutRequestFromForm(form: FormData, cart: Cart): { value: MemberCheckoutRequest | null; errors: FieldErrors } {
  const errors: FieldErrors = {};
  const shippingAddressId = formValue(form, "shippingAddressId");
  const shippingMethodId = formValue(form, "shippingMethodId");
  const couponCode = formValue(form, "couponCode");
  if (!shippingAddressId) errors.shippingAddressId = "Bir teslimat adresi seçin.";
  if (!shippingMethodId) errors.shippingMethodId = "Bir kargo yöntemi seçin.";
  if (couponCode.length > 50) errors.couponCode = "Kupon kodu en fazla 50 karakter olabilir.";

  if (Object.keys(errors).length || !cart.concurrencyToken || !shippingAddressId || !shippingMethodId) {
    return { value: null, errors };
  }

  return {
    value: {
      expectedCartConcurrencyToken: cart.concurrencyToken,
      shippingAddressId,
      shippingMethodId,
      ...(couponCode ? { couponCode } : {}),
    },
    errors,
  };
}

function addressFromForm(form: FormData, prefix: "shipping" | "billing", errors: FieldErrors, fallbackPhoneNumber: string): GuestAddressRequest | null {
  const title = requiredFormValue(form, `${prefix}Title`, "Adres başlığı", 100, errors);
  const firstName = requiredFormValue(form, `${prefix}FirstName`, "Alıcı adı", 100, errors);
  const lastName = requiredFormValue(form, `${prefix}LastName`, "Alıcı soyadı", 100, errors);
  const phoneNumber = fallbackPhoneNumber;
  const city = requiredFormValue(form, `${prefix}City`, "İl", 100, errors);
  const district = requiredFormValue(form, `${prefix}District`, "İlçe", 100, errors);
  const neighborhood = formValue(form, `${prefix}Neighborhood`);
  if (neighborhood.length > 100) errors[`${prefix}Neighborhood`] = "Mahalle en fazla 100 karakter olabilir.";
  const fullAddress = requiredFormValue(form, `${prefix}FullAddress`, "Açık adres", 500, errors);
  const postalCode = formValue(form, `${prefix}PostalCode`);
  if (postalCode.length > 20) errors[`${prefix}PostalCode`] = "Posta kodu en fazla 20 karakter olabilir.";

  return title && firstName && lastName && phoneNumber && city && district && fullAddress
    ? { title, firstName, lastName, phoneNumber, city, district, ...(neighborhood ? { neighborhood } : {}), fullAddress, ...(postalCode ? { postalCode } : {}) }
    : null;
}

function requiredFormValue(form: FormData, name: string, label: string, maximumLength: number, errors: FieldErrors): string {
  const value = formValue(form, name);
  if (!value) errors[name] = `${label} alanını doldurun.`;
  else if (value.length > maximumLength) errors[name] = `${label} en fazla ${maximumLength} karakter olabilir.`;
  return value;
}

function formValue(form: FormData, name: string): string {
  const value = form.get(name);
  return typeof value === "string" ? value.trim() : "";
}

function mapApiFieldErrors(errors: Record<string, string[]> | undefined): FieldErrors {
  if (!errors) return {};
  const fieldMap: Record<string, string> = {
    "Customer.FirstName": "customerFirstName",
    "Customer.LastName": "customerLastName",
    "Customer.Email": "customerEmail",
    "Customer.PhoneNumber": "customerPhoneNumber",
    "ShippingAddress.Title": "shippingTitle",
    "ShippingAddress.FirstName": "shippingFirstName",
    "ShippingAddress.LastName": "shippingLastName",
    "ShippingAddress.PhoneNumber": "shippingPhoneNumber",
    "ShippingAddress.City": "shippingCity",
    "ShippingAddress.District": "shippingDistrict",
    "ShippingAddress.Neighborhood": "shippingNeighborhood",
    "ShippingAddress.FullAddress": "shippingFullAddress",
    "ShippingAddress.PostalCode": "shippingPostalCode",
    ShippingMethodId: "shippingMethodId",
    CouponCode: "couponCode",
  };

  return Object.fromEntries(
    Object.entries(errors).flatMap(([key, messages]) => {
      const field = fieldMap[key];
      return field && messages[0] ? [[field, messages[0]]] : [];
    }),
  );
}

function formatMoney(value: number, currency: string): string {
  return new Intl.NumberFormat("tr-TR", { style: "currency", currency, minimumFractionDigits: 2 }).format(value);
}
