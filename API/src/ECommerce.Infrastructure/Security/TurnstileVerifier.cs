using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ECommerce.Application.Common.Security;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Infrastructure.Security;

public sealed class TurnstileVerifier : ITurnstileVerifier
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    // Burada kısa timeout'lu HTTP istemcisi ve secret yapılandırmasıyla Turnstile doğrulayıcıyı hazırlıyorum.
    public TurnstileVerifier(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    // Burada Turnstile tokenını yalnız Cloudflare siteverify endpointinde doğruluyorum.
    public async Task<TurnstileVerificationResult> VerifyAsync(
        string token,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        return await VerifyCoreAsync(token, ipAddress, null, null, cancellationToken);
    }

    // Burada contact form tokenını beklenen action ve hostname ile birlikte doğruluyorum.
    public async Task<TurnstileVerificationResult> VerifyAsync(
        string token,
        string? ipAddress,
        string expectedAction,
        string expectedHostname,
        CancellationToken cancellationToken = default)
    {
        return await VerifyCoreAsync(token, ipAddress, expectedAction, expectedHostname, cancellationToken);
    }

    // Burada ortak siteverify çağrısını opsiyonel action ve hostname eşleşmesiyle sonuçlandırıyorum.
    private async Task<TurnstileVerificationResult> VerifyCoreAsync(
        string token,
        string? ipAddress,
        string? expectedAction,
        string? expectedHostname,
        CancellationToken cancellationToken)
    {
        var secret = expectedAction is null
            ? _configuration["GuestProtection:Turnstile:SecretKey"]
            : _configuration["ContactProtection:Turnstile:SecretKey"] ?? _configuration["GuestProtection:Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(token) || token.Length > 2048)
        {
            return string.IsNullOrWhiteSpace(secret)
                ? TurnstileVerificationResult.Unavailable
                : TurnstileVerificationResult.Invalid;
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "turnstile/v0/siteverify",
                new TurnstileRequest(secret, token, ipAddress, Guid.NewGuid()),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return TurnstileVerificationResult.Unavailable;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(cancellationToken);
            var contextMatches = expectedAction is null ||
                (string.Equals(result?.Action, expectedAction, StringComparison.Ordinal) &&
                 string.Equals(result?.Hostname, expectedHostname, StringComparison.OrdinalIgnoreCase));
            return result?.Success == true && contextMatches
                ? TurnstileVerificationResult.Valid
                : TurnstileVerificationResult.Invalid;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return TurnstileVerificationResult.Unavailable;
        }
    }

    // Burada siteverify JSON isteğinin alan adlarını Cloudflare sözleşmesine eşliyorum.
    private sealed record TurnstileRequest(
        [property: JsonPropertyName("secret")] string Secret,
        [property: JsonPropertyName("response")] string Response,
        [property: JsonPropertyName("remoteip")] string? RemoteIp,
        [property: JsonPropertyName("idempotency_key")] Guid IdempotencyKey);

    // Burada siteverify cevabından yalnız güvenlik kararı için gereken success alanını okuyorum.
    private sealed record TurnstileResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("action")] string? Action,
        [property: JsonPropertyName("hostname")] string? Hostname);
}
