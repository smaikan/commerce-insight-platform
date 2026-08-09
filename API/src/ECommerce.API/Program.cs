using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Persistence;
using ECommerce.API.ErrorHandling;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Security.Claims;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Orders.Services;
using ECommerce.API.Security;
using System.Threading.RateLimiting;
using ECommerce.API.BackgroundServices;
using ECommerce.API.Configuration;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("public-products", policy => policy
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByQuery("*")
        .Tag("products"));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHostedService<UserTokenCleanupBackgroundService>();
builder.Services.AddHostedService<EmailOutboxBackgroundService>();
builder.Services.AddOptions<OrderReservationOptions>()
    .Bind(builder.Configuration.GetSection(OrderReservationOptions.SectionName))
    .Validate(
        options => options.DurationMinutes is >= 1 and <= 10_080,
        "Order reservation duration must be between 1 minute and 7 days.")
    .Validate(
        options => options.SweepIntervalSeconds is >= 5 and <= 3_600,
        "Order reservation sweep interval must be between 5 seconds and 1 hour.")
    .Validate(
        options => options.BatchSize is >= 1 and <= 500,
        "Order reservation batch size must be between 1 and 500.")
    .ValidateOnStart();
builder.Services.AddSingleton<IOrderReservationPolicy>(provider =>
    new ConfigurationOrderReservationPolicy(
        provider.GetRequiredService<IOptions<OrderReservationOptions>>().Value));
builder.Services.AddHostedService<OrderReservationExpirationBackgroundService>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        ApiProblemDetailsResponse.Enrich(context.ProblemDetails, context.HttpContext);
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    const string bearerScheme = "Bearer";

    // Burada null olamaz C# referans alanlarını Swagger sözleşmesinde de zorunlu gösteriyorum.
    options.SupportNonNullableReferenceTypes();
    options.NonNullableReferenceTypesAsRequired();
    options.CustomSchemaIds(type =>
    {
        if (type.Name is "Payment" or "PaymentStatus" or "PaymentDto")
        {
            return type.FullName?.Replace("+", ".") ?? type.Name;
        }

        if (!type.IsConstructedGenericType)
        {
            return type.Name.Replace("[]", "Array");
        }

        var argumentNames = string.Concat(type.GetGenericArguments()
            .Select(argument => argument.Name.Replace("[]", "Array")));
        return argumentNames + type.Name.Split('`')[0];
    });

    options.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter the JWT access token returned by the login endpoint."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(bearerScheme, document, null)] = []
    });
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
if (!builder.Environment.IsDevelopment() &&
    string.IsNullOrWhiteSpace(builder.Configuration["DataProtection:KeyRingPath"]))
{
    throw new InvalidOperationException(
        "DataProtection:KeyRingPath must point to a shared persistent location outside development.");
}

builder.Services.AddInfrastructureServices(builder.Configuration);

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? string.Empty;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (Encoding.UTF8.GetByteCount(jwtSecretKey) < 32)
{
    throw new InvalidOperationException(
        "JWT secret key must be configured and at least 32 bytes long. " +
        "Use User Secrets in development or a secure secret store in production.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT issuer and audience must be configured.");
}

// JWT doğrulamasını burada API girişinde bağlıyorum; token üretimi Infrastructure içinde kalıyor.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                var hasAuthenticationFailure = context.AuthenticateFailure is not null;
                var detail = hasAuthenticationFailure
                    ? builder.Environment.IsDevelopment()
                        ? $"Access token validation failed: {context.AuthenticateFailure!.Message}"
                        : "The access token is invalid or expired."
                    : "A valid access token is required.";
                var errorCode = hasAuthenticationFailure
                    ? ApiErrorCodes.InvalidAccessToken
                    : ApiErrorCodes.AuthenticationRequired;

                var problemDetails = ApiProblemDetailsResponse.Create(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    hasAuthenticationFailure ? "Invalid access token" : "Authentication required",
                    detail,
                    errorCode);

                await ApiProblemDetailsResponse.WriteAsync(
                    context.HttpContext,
                    problemDetails,
                    context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                var problemDetails = ApiProblemDetailsResponse.Create(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    "You do not have permission to perform this operation.",
                    ApiErrorCodes.Forbidden);

                await ApiProblemDetailsResponse.WriteAsync(
                    context.HttpContext,
                    problemDetails,
                    context.HttpContext.RequestAborted);
            },
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var securityVersionValue = context.Principal?.FindFirstValue(AuthClaimTypes.SecurityVersion);
                var sessionIdValue = context.Principal?.FindFirstValue(AuthClaimTypes.SessionId);

                if (!PublicIdCodec.TryDecodeUserId(userIdValue, out var userId) ||
                    !int.TryParse(securityVersionValue, out var securityVersion) ||
                    !Guid.TryParse(sessionIdValue, out var sessionId))
                {
                    context.Fail("Access token claims are invalid.");
                    return;
                }

                var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var dateTimeProvider = context.HttpContext.RequestServices.GetRequiredService<IDateTimeProvider>();
                var isValid = await userRepository.IsAccessTokenValidAsync(
                    userId,
                    securityVersion,
                    sessionId,
                    dateTimeProvider.UtcNow,
                    context.HttpContext.RequestAborted);

                if (!isValid)
                {
                    context.Fail("Access token is no longer valid.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.AdminOnly,
        policy => policy.RequireRole(UserRole.Admin.ToString()));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("cart", httpContext =>
    {
        var userKey = httpContext.User.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        var clientKey = userKey ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.AddPolicy("orders", httpContext =>
    {
        var clientKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            clientKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.AddPolicy("payments", httpContext =>
    {
        var clientKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            clientKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var isSensitiveAuthPath = path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/forgot-password", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/refresh-token", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/reset-password", StringComparison.OrdinalIgnoreCase);

        var clientKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (isSensitiveAuthPath)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"auth:{clientKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        return RateLimitPartition.GetNoLimiter("non-sensitive-endpoint");
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var httpContext = context.HttpContext;
    var (title, detail, errorCode) = ApiProblemDetailsResponse.DescribeStatusCode(httpContext.Response.StatusCode);
    var problemDetails = ApiProblemDetailsResponse.Create(
        httpContext,
        httpContext.Response.StatusCode,
        title,
        detail,
        errorCode);

    await ApiProblemDetailsResponse.WriteAsync(
        httpContext,
        problemDetails,
        httpContext.RequestAborted);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseResponseCompression();
app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseOutputCache();

app.MapControllers();

app.Run();

public partial class Program
{
}
