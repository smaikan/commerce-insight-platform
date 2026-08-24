namespace ECommerce.API.ErrorHandling;

public static class ApiErrorCodes
{
    public const string Validation = "validation_error";
    public const string BusinessRule = "business_rule_violation";
    public const string NotFound = "resource_not_found";
    public const string Conflict = "conflict";
    public const string Concurrency = "concurrency_conflict";
    public const string ReturnStatusTransitionInvalid = "return_status_transition_invalid";
    public const string Unauthorized = "unauthorized";
    public const string AuthenticationRequired = "authentication_required";
    public const string InvalidAccessToken = "invalid_access_token";
    public const string Forbidden = "forbidden";
    public const string RateLimitExceeded = "rate_limit_exceeded";
    public const string BadRequest = "bad_request";
    public const string PayloadTooLarge = "payload_too_large";
    public const string Internal = "internal_error";
    public const string CouponMembersOnly = "coupon_members_only";
    public const string GuestCheckoutChallengeRequired = "guest_checkout_challenge_required";
    public const string GuestCheckoutRateLimited = "guest_checkout_rate_limited";
    public const string GuestCheckoutProtectionUnavailable = "guest_checkout_protection_unavailable";
    public const string IdempotencyKeyReused = "idempotency_key_reused";
    public const string InvalidGuestAccess = "invalid_guest_access";
    public const string ContactChallengeRequired = "contact_challenge_required";
    public const string ContactSubmissionRateLimited = "contact_submission_rate_limited";
    public const string ContactProtectionUnavailable = "contact_protection_unavailable";
}
