using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Persistence;
using ECommerce.API.ErrorHandling;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Security.Claims;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Common.Identifiers;
using ECommerce.API.Security;
using System.Threading.RateLimiting;
using ECommerce.API.BackgroundServices;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHostedService<UserTokenCleanupBackgroundService>();
builder.Services.AddHostedService<EmailOutboxBackgroundService>();
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
builder.Services.AddInfrastructureServices();

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
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var isSensitiveAuthPath = path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/forgot-password", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/refresh-token", StringComparison.OrdinalIgnoreCase);

        if (!isSensitiveAuthPath)
        {
            return RateLimitPartition.GetNoLimiter("unlimited");
        }

        var clientKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            clientKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
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

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
