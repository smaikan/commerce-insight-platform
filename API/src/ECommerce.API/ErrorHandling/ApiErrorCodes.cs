namespace ECommerce.API.ErrorHandling;

public static class ApiErrorCodes
{
    public const string Validation = "validation_error";
    public const string BusinessRule = "business_rule_violation";
    public const string NotFound = "resource_not_found";
    public const string Conflict = "conflict";
    public const string Concurrency = "concurrency_conflict";
    public const string Unauthorized = "unauthorized";
    public const string AuthenticationRequired = "authentication_required";
    public const string InvalidAccessToken = "invalid_access_token";
    public const string Forbidden = "forbidden";
    public const string RateLimitExceeded = "rate_limit_exceeded";
    public const string BadRequest = "bad_request";
    public const string Internal = "internal_error";
}
