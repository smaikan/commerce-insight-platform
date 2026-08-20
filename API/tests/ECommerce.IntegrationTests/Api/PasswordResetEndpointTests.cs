using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ECommerce.API.Configuration;
using ECommerce.API.BackgroundServices;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Email;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;

namespace ECommerce.IntegrationTests.Api;

public sealed class PasswordResetEndpointTests
{
    private const string ExistingEmail = "reset-user@test.local";
    private const string InitialPassword = "Initial123!";
    private const string NewPassword = "Changed123!";
    private const string RawResetToken = "deterministic-reset-token";

    // Burada var ve yok e-postaların aynı cevabı verdiğini, cooldown'ın yeni token üretmediğini doğruluyorum.
    [Fact]
    public async Task ForgotPassword_Should_Be_Uniform_Atomic_And_Cooldown_Safe()
    {
        await using var factory = new PasswordResetApiFactory();
        await factory.InitializeAndSeedUserAsync();
        using var client = factory.CreatePasswordResetClient();

        var existingResponse = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = ExistingEmail });
        var missingResponse = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = "missing@test.local" });
        var cooldownResponse = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = ExistingEmail });

        existingResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        missingResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        cooldownResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await existingResponse.Content.ReadAsStringAsync()).Should().BeEmpty();
        (await missingResponse.Content.ReadAsStringAsync()).Should().BeEmpty();
        (await cooldownResponse.Content.ReadAsStringAsync()).Should().BeEmpty();

        var state = await factory.ReadResetStateAsync();
        state.SecurityTokenCount.Should().Be(1);
        state.ActiveSecurityTokenCount.Should().Be(1);
        state.OutboxCount.Should().Be(1);
        state.ProtectedToken.Should().NotBeNullOrWhiteSpace();
        (await factory.ReadRawOutboxTokenAsync()).Should().NotBeNullOrWhiteSpace();
    }

    // Burada geçersiz ve süresi dolmuş tokenların aynı güvenli ProblemDetails kodunu döndürdüğünü doğruluyorum.
    [Fact]
    public async Task ResetPassword_Should_Return_Stable_ProblemDetails_For_Invalid_Or_Expired_Token()
    {
        await using var factory = new PasswordResetApiFactory();
        await factory.InitializeAndSeedUserAsync();
        await factory.SeedExpiredResetTokenAsync("expired-reset-token");
        using var client = factory.CreatePasswordResetClient();

        var invalidResponse = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = "unknown-reset-token", newPassword = NewPassword });
        var expiredResponse = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = "expired-reset-token", newPassword = NewPassword });

        await AssertProblemDetailsAsync(
            invalidResponse,
            HttpStatusCode.Unauthorized,
            "invalid_or_expired_reset_token");
        await AssertProblemDetailsAsync(
            expiredResponse,
            HttpStatusCode.Unauthorized,
            "invalid_or_expired_reset_token");
    }

    // Burada iki endpointin geçersiz request gövdelerini ortak 400 ProblemDetails sözleşmesiyle reddettiğini doğruluyorum.
    [Fact]
    public async Task Password_Reset_Validation_Should_Return_ProblemDetails()
    {
        await using var factory = new PasswordResetApiFactory();
        using var client = factory.CreatePasswordResetClient();

        var forgotResponse = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = "not-an-email" });
        var resetResponse = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = string.Empty, newPassword = "short" });

        await AssertProblemDetailsAsync(forgotResponse, HttpStatusCode.BadRequest, "bad_request");
        await AssertProblemDetailsAsync(resetResponse, HttpStatusCode.BadRequest, "bad_request");
    }

    // Burada başarılı resetin tokenı tek kullanımlık yaptığını ve bütün oturumları geçersiz kıldığını doğruluyorum.
    [Fact]
    public async Task ResetPassword_Should_Revoke_Sessions_And_Invalidate_Old_Access_Token()
    {
        await using var factory = new PasswordResetApiFactory();
        await factory.InitializeAndSeedUserAsync();
        using var client = factory.CreatePasswordResetClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = ExistingEmail, password = InitialPassword, deviceName = "integration-test" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessToken = await ReadAccessTokenAsync(loginResponse);

        (await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = ExistingEmail })).StatusCode.Should().Be(HttpStatusCode.Accepted);
        var emailedResetToken = await factory.ReadRawOutboxTokenAsync();
        var resetResponse = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = emailedResetToken, newPassword = NewPassword });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var replayResponse = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = emailedResetToken, newPassword = "Another123!" });
        await AssertProblemDetailsAsync(
            replayResponse,
            HttpStatusCode.Unauthorized,
            "invalid_or_expired_reset_token");

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var oldAccessResponse = await client.SendAsync(meRequest);
        await AssertProblemDetailsAsync(oldAccessResponse, HttpStatusCode.Unauthorized, "invalid_access_token");

        (await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = ExistingEmail, password = InitialPassword })).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = ExistingEmail, password = NewPassword })).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var state = await factory.ReadResetStateAsync();
        state.UsedSecurityTokenCount.Should().Be(1);
        state.ActiveRefreshTokenCount.Should().Be(1, "yeni parola ile son login yeni bir oturum oluşturur");
        state.RevokedRefreshTokenCount.Should().Be(1);
    }

    // Burada aynı tek kullanımlık tokenla paralel resetlerden yalnız birinin başarılı olduğunu doğruluyorum.
    [Fact]
    public async Task Parallel_Reset_With_The_Same_Token_Should_Allow_Only_One_Success()
    {
        await using var factory = new PasswordResetApiFactory();
        await factory.InitializeAndSeedUserAsync();
        await factory.SeedActiveResetTokenAsync(RawResetToken);
        using var firstClient = factory.CreatePasswordResetClient();
        using var secondClient = factory.CreatePasswordResetClient();

        var firstTask = firstClient.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = RawResetToken, newPassword = NewPassword });
        var secondTask = secondClient.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = RawResetToken, newPassword = "Parallel123!" });
        var responses = await Task.WhenAll(firstTask, secondTask);

        responses.Count(response => response.StatusCode == HttpStatusCode.NoContent).Should().Be(1);
        responses.Single(response => response.StatusCode != HttpStatusCode.NoContent).StatusCode
            .Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Conflict);
        (await factory.ReadResetStateAsync()).UsedSecurityTokenCount.Should().Be(1);
    }

    // Burada parola sıfırlama IP limitinin 429 ve standart ProblemDetails ürettiğini doğruluyorum.
    [Fact]
    public async Task PasswordReset_Rate_Limit_Should_Return_ProblemDetails()
    {
        await using var factory = new PasswordResetApiFactory();
        await factory.InitializeAndSeedUserAsync();
        using var client = factory.CreatePasswordResetClient();

        for (var index = 0; index < 5; index++)
        {
            var allowedResponse = await client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                new { email = $"missing-{index}@test.local" });
            allowedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        var limitedResponse = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = "limited@test.local" });

        await AssertProblemDetailsAsync(limitedResponse, HttpStatusCode.TooManyRequests, "rate_limit_exceeded");
    }

    // Burada OpenAPI'nin gerçek 202/204 başarıları ile 400/401/409/429 hata sözleşmelerini yayımladığını doğruluyorum.
    [Fact]
    public async Task OpenApi_Should_Describe_Password_Reset_Contracts()
    {
        await using var factory = new PasswordResetApiFactory();
        using var client = factory.CreatePasswordResetClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");

        var forgotOperation = paths.GetProperty("/api/auth/forgot-password").GetProperty("post");
        forgotOperation.GetProperty("responses").EnumerateObject().Select(item => item.Name)
            .Should().BeEquivalentTo(["202", "400", "429"]);
        forgotOperation.GetProperty("security").GetArrayLength().Should().Be(0);

        var resetOperation = paths.GetProperty("/api/auth/reset-password").GetProperty("post");
        resetOperation.GetProperty("responses").EnumerateObject().Select(item => item.Name)
            .Should().BeEquivalentTo(["204", "400", "401", "409", "429"]);
        resetOperation.GetProperty("security").GetArrayLength().Should().Be(0);
    }

    // Burada reset tokenının query string yerine URL fragment'ına yerleştirildiğini doğruluyorum.
    [Fact]
    public void Password_Reset_Email_Link_Should_Keep_Token_Out_Of_Query_String()
    {
        var method = typeof(SmtpEmailSender).GetMethod(
            "BuildPasswordResetLink",
            BindingFlags.Static | BindingFlags.NonPublic);

        var link = method!.Invoke(null, ["https://store.test/reset-password?source=email", "token value"]) as string;
        var uri = new Uri(link!);

        uri.Query.Should().Be("?source=email");
        uri.Fragment.Should().Be("#token=token%20value");
        uri.Query.Should().NotContain("token value").And.NotContain("token=");
    }

    // Burada gerçek SMTP protokolü kullanan sahte sunucuda HTML e-postasının fragment bağlantısını taşıdığını doğruluyorum.
    [Fact]
    public async Task Password_Reset_Email_Should_Send_Fragment_Link_Through_Smtp()
    {
        await using var smtpServer = new FakeSmtpServer();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:PasswordResetUrl"] = "https://store.test/reset-password?source=email",
                ["Email:FromAddress"] = "noreply@store.test",
                ["Email:FromName"] = "Store",
                ["Email:Smtp:Host"] = "127.0.0.1",
                ["Email:Smtp:Port"] = smtpServer.Port.ToString(),
                ["Email:Smtp:UseSsl"] = "false"
            })
            .Build();
        var mockStoreSettingsRepo = new Moq.Mock<IStoreSettingsRepository>();
        var sender = new SmtpEmailSender(configuration, mockStoreSettingsRepo.Object);

        await sender.SendPasswordResetAsync(
            "customer@test.local",
            "smtp-reset-token",
            DateTime.UtcNow.AddMinutes(30));
        var message = await smtpServer.ReceiveMessageAsync();
        var bodySeparatorIndex = message.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        var encodedBody = message[(bodySeparatorIndex + 4)..]
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
        var htmlBody = Encoding.UTF8.GetString(Convert.FromBase64String(encodedBody));

        htmlBody.Should().Contain("https://store.test/reset-password?source=email#token=smtp-reset-token");
        htmlBody.Should().NotContain("?token=smtp-reset-token");
    }

    // Burada SMTP hata kaydının alıcı e-postası veya ham tokenı loglanabilir metne taşımadığını doğruluyorum.
    [Fact]
    public void Email_Delivery_Error_Should_Be_Safely_Fingerprinted()
    {
        var method = typeof(EmailOutboxBackgroundService).GetMethod(
            "CreateSafeDeliveryError",
            BindingFlags.Static | BindingFlags.NonPublic);
        var safeError = method!.Invoke(
            null,
            [new InvalidOperationException("customer@test.local smtp-reset-token")]) as string;

        safeError.Should().StartWith("InvalidOperationException:");
        safeError.Should().NotContain("customer@test.local");
        safeError.Should().NotContain("smtp-reset-token");
    }

    // Burada production ortamının localhost reset URL'si ve eksik SMTP secret ile başlamayı reddettiğini doğruluyorum.
    [Fact]
    public void Production_Email_Configuration_Should_Reject_Unsafe_Values()
    {
        var validator = new EmailDeliveryOptionsValidator(new StubHostEnvironment("Production"));

        var result = validator.Validate(null, new EmailDeliveryOptions
        {
            FromAddress = "store@test.local",
            PasswordResetUrl = "http://localhost:3000/reset-password",
            Smtp = new SmtpDeliveryOptions
            {
                Host = "smtp.test.local",
                Port = 587,
                Username = "smtp-user",
                Password = string.Empty
            }
        });

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(item => item.Contains("HTTPS", StringComparison.Ordinal));
        result.Failures.Should().Contain(item => item.Contains("localhost", StringComparison.Ordinal));
        result.Failures.Should().Contain(item => item.Contains("secret store", StringComparison.Ordinal));
    }

    // Burada production için güvenli URL ve secret-store değeri sağlandığında başlangıç doğrulamasının geçtiğini doğruluyorum.
    [Fact]
    public void Production_Email_Configuration_Should_Accept_Safe_Values()
    {
        var validator = new EmailDeliveryOptionsValidator(new StubHostEnvironment("Production"));

        var result = validator.Validate(null, new EmailDeliveryOptions
        {
            FromAddress = "store@test.local",
            PasswordResetUrl = "https://store.test/reset-password",
            Smtp = new SmtpDeliveryOptions
            {
                Host = "smtp.test.local",
                Port = 587,
                Username = "smtp-user",
                Password = "secret-from-test-store"
            }
        });

        result.Succeeded.Should().BeTrue();
    }

    // Burada HTTP hata gövdesinin ortak code ve traceId alanlarını taşıdığını doğruluyorum.
    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        response.StatusCode.Should().Be(expectedStatus);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be(expectedCode);
        document.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    // Burada login cevabındaki access tokenı başka HTTP doğrulamalarında kullanmak üzere okuyorum.
    private static async Task<string> ReadAccessTokenAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;
    }

    private sealed class PasswordResetApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"ecommerce-password-reset-{Guid.NewGuid():N}.db");

        // Burada API test sunucusunu ayrı SQLite dosyası ve sabit reset tokenı ile yapılandırıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "unused-by-test-override");
            builder.UseSetting("Jwt:Issuer", "ECommerce.PasswordResetTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.PasswordResetTests.Client");
            builder.UseSetting("Jwt:SecretKey", "password-reset-integration-secret-key-at-least-32-bytes");
            builder.UseSetting("Auth:PasswordResetRequestCooldownSeconds", "120");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), "ECommerce.PasswordResetTests.Keys"));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                foreach (var hostedService in services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                    .ToList())
                {
                    services.Remove(hostedService);
                }

                services.AddDbContext<AppDbContext>(options => options.UseSqlite(
                    $"Data Source={_databasePath};Cache=Shared;Default Timeout=30"));
            });
        }

        // Burada cookie veya yönlendirme yönetmeyen HTTPS tabanlı gerçek HTTP istemcisini oluşturuyorum.
        public HttpClient CreatePasswordResetClient()
        {
            return CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = false
            });
        }

        // Burada aktif test kullanıcısını gerçek parola hasherı ile veritabanına hazırlıyorum.
        public async Task InitializeAndSeedUserAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            context.Users.Add(new User(
                ExistingEmail,
                passwordHasher.Hash(InitialPassword),
                "Reset",
                "User"));
            await context.SaveChangesAsync();
        }

        // Burada paralel kullanım testi için bilinen ham değere bağlı aktif reset tokenı oluşturuyorum.
        public async Task SeedActiveResetTokenAsync(string rawToken)
        {
            await SeedResetTokenAsync(rawToken, DateTime.UtcNow.AddMinutes(30));
        }

        // Burada süresi dolmuş tokenın HTTP hata sözleşmesini sınamak için eski kayıt oluşturuyorum.
        public async Task SeedExpiredResetTokenAsync(string rawToken)
        {
            await SeedResetTokenAsync(rawToken, DateTime.UtcNow.AddMinutes(-1));
        }

        // Burada reset ve oturum kayıtlarının kalıcı durumunu tek okumada özetliyorum.
        public async Task<ResetState> ReadResetStateAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var utcNow = DateTime.UtcNow;
            var securityTokens = await context.UserSecurityTokens.AsNoTracking().ToListAsync();
            var refreshTokens = await context.UserRefreshTokens.AsNoTracking().ToListAsync();
            var outbox = await context.EmailOutbox.AsNoTracking().SingleOrDefaultAsync();
            return new ResetState(
                securityTokens.Count,
                securityTokens.Count(token => token.CanBeUsed(utcNow)),
                securityTokens.Count(token => token.UsedAt.HasValue),
                await context.EmailOutbox.AsNoTracking().CountAsync(),
                outbox?.ProtectedToken,
                refreshTokens.Count(token => token.RevokedAt is null && token.ExpiresAt > utcNow),
                refreshTokens.Count(token => token.RevokedAt.HasValue));
        }

        // Burada outbox'taki korumalı değeri yalnız test kapsamında çözüp gerçek e-posta tokenını elde ediyorum.
        public async Task<string> ReadRawOutboxTokenAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var protectedToken = await context.EmailOutbox.AsNoTracking()
                .Select(message => message.ProtectedToken)
                .SingleAsync();
            return scope.ServiceProvider.GetRequiredService<IPasswordResetTokenProtector>()
                .Unprotect(protectedToken!);
        }

        // Burada istenen geçerlilik süresine sahip reset tokenını gerçek hash servisiyle kaydediyorum.
        private async Task SeedResetTokenAsync(string rawToken, DateTime expiresAt)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await context.Users.SingleAsync(item => item.Email == ExistingEmail);
            var tokenHasher = scope.ServiceProvider.GetRequiredService<ITokenHasher>();
            var createdAt = expiresAt > DateTime.UtcNow
                ? DateTime.UtcNow
                : expiresAt.AddMinutes(-30);
            context.UserSecurityTokens.Add(new UserSecurityToken(
                user.Id,
                UserSecurityTokenType.PasswordReset,
                tokenHasher.Hash(rawToken),
                expiresAt,
                createdAt));
            await context.SaveChangesAsync();
        }

        // Burada test veritabanı dosyasını host kapandıktan sonra temizliyorum.
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            SqliteConnection.ClearAllPools();
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
    }

    private sealed record ResetState(
        int SecurityTokenCount,
        int ActiveSecurityTokenCount,
        int UsedSecurityTokenCount,
        int OutboxCount,
        string? ProtectedToken,
        int ActiveRefreshTokenCount,
        int RevokedRefreshTokenCount);

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        // Burada options validator testine gerekli ortam adını ve zararsız dosya sağlayıcısını hazırlıyorum.
        public StubHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "ECommerce.PasswordResetTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeSmtpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly Task<string> _messageTask;

        // Burada tek e-posta yakalayacak yerel SMTP dinleyicisini rastgele boş bir portta başlatıyorum.
        public FakeSmtpServer()
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _messageTask = CaptureMessageAsync();
        }

        public int Port { get; }

        // Burada SMTP istemcisinden yakalanan ham MIME mesajını teste döndürüyorum.
        public Task<string> ReceiveMessageAsync() => _messageTask;

        // Burada test tamamlandığında yerel SMTP soketini güvenli biçimde kapatıyorum.
        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            if (!_messageTask.IsCompleted)
            {
                await _messageTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }

        // Burada temel SMTP komutlarını cevaplayıp DATA bölümündeki MIME içeriğini topluyorum.
        private async Task<string> CaptureMessageAsync()
        {
            using var client = await _listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true)
            {
                AutoFlush = true,
                NewLine = "\r\n"
            };
            await writer.WriteLineAsync("220 localhost test smtp");

            var message = new StringBuilder();
            var readingData = false;
            while (await reader.ReadLineAsync() is { } line)
            {
                if (readingData)
                {
                    if (line == ".")
                    {
                        readingData = false;
                        await writer.WriteLineAsync("250 queued");
                    }
                    else
                    {
                        message.AppendLine(line);
                    }

                    continue;
                }

                if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    readingData = true;
                    await writer.WriteLineAsync("354 end with dot");
                }
                else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("221 bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 ok");
                }
            }

            return message.ToString();
        }
    }
}
