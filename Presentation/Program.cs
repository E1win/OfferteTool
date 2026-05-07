using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.SecurityAudit;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Configuration;
using Infrastructure.Email;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Presentation.Builders;
using Presentation.Models.Api;
using Presentation.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("De applicatie is niet goed geconfigureerd.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Currently set to false, because accounts are all created by administrator
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password options
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // User options
    options.User.RequireUniqueEmail = true;

    // Account lockout options
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    // Set expiration to 4 hours with sliding expiration, so users won't be logged out in the middle of filling out a tender form
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
    options.SlidingExpiration = true;

    // Set secure cookie settings
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // Return JSON responses for API requests instead of redirecting to login or access denied pages
    options.Events.OnRedirectToLogin = async context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Redirect(context.RedirectUri);
            return;
        }

        await TryLogAccessDeniedAsync(context.HttpContext, SecurityAuditOutcome.Denied);

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ApiResponse<object?>
        {
            Data = null,
            Message = "Uw sessie is verlopen. Meld u opnieuw aan en probeer het daarna nog eens.",
            Errors = []
        });
    };

    options.Events.OnRedirectToAccessDenied = async context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await TryLogAccessDeniedAsync(context.HttpContext, SecurityAuditOutcome.Denied);
            context.Response.Redirect(context.RedirectUri);
            return;
        }

        await TryLogAccessDeniedAsync(context.HttpContext, SecurityAuditOutcome.Denied);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ApiResponse<object?>
        {
            Data = null,
            Message = "U heeft geen toestemming om deze actie uit te voeren.",
            Errors = []
        });
    };
});

builder.Services.AddScoped<ITenderRepository, TenderRepository>();
builder.Services.AddScoped<ITenderQuestionRepository, TenderQuestionRepository>();
builder.Services.AddScoped<ITenderSubmissionRepository, TenderSubmissionRepository>();
builder.Services.AddScoped<ITenderSubmissionReviewRepository, TenderSubmissionReviewRepository>();
builder.Services.AddScoped<ITenderChangeLogRepository, TenderChangeLogRepository>();
builder.Services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
builder.Services.AddScoped<IOrganisationRepository, OrganisationRepository>();
builder.Services.Configure<TenderSubmissionEncryptionOptions>(
    builder.Configuration.GetSection(TenderSubmissionEncryptionOptions.SectionName));
builder.Services.Configure<SmtpEmailOptions>(
    builder.Configuration.GetSection(SmtpEmailOptions.SectionName));

builder.Services.AddScoped<ITenderService, TenderService>();
builder.Services.AddScoped<ITenderComparisonService, TenderComparisonService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITenderQuestionService, TenderQuestionService>();
builder.Services.AddScoped<ITenderReviewerService, TenderReviewerService>();
builder.Services.AddScoped<ITenderReviewService, TenderReviewService>();
builder.Services.AddScoped<ITenderSubmissionService, TenderSubmissionService>();
builder.Services.AddScoped<ITenderChangeLogService, TenderChangeLogService>();
builder.Services.AddScoped<ITenderChangeNotificationService, TenderChangeNotificationService>();
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IOrganisationManagementService, OrganisationManagementService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ITenderSubmissionEncryptionService, AesTenderSubmissionEncryptionService>();
builder.Services.AddSingleton<TenderAnswerPayloadSerializer>();
builder.Services.AddScoped<ITenderPageModelBuilder, TenderPageModelBuilder>();
builder.Services.AddScoped<ITenderComparisonPageModelBuilder, TenderComparisonPageModelBuilder>();
builder.Services.AddScoped<ITenderReviewPageModelBuilder, TenderReviewPageModelBuilder>();
builder.Services.AddScoped<IUserManagementPageModelBuilder, UserManagementPageModelBuilder>();
builder.Services.AddScoped<IOrganisationManagementPageModelBuilder, OrganisationManagementPageModelBuilder>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Voer een geldige waarde in.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(_ => "Vul een geldig getal in.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Dit veld is verplicht.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, fieldName) => $"{fieldName} bevat geen geldige waarde.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(fieldName => $"{fieldName} is verplicht.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "Deze waarde ontbreekt.");
    options.Filters.Add<ServiceExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddRateLimiter(options =>
{
    const string loginPath = "/Identity/Account/Login";
    const string createTenderPath = "/Tender/Create";

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        if (context.HttpContext.Response.HasStarted)
            return;

        context.HttpContext.Response.ContentType = context.HttpContext.Request.Path.StartsWithSegments("/api")
            ? "application/json"
            : "text/plain; charset=utf-8";

        if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            await context.HttpContext.Response.WriteAsJsonAsync(new ApiResponse<object?>
            {
                Data = null,
                Message = "Er zijn in korte tijd te veel verzoeken verstuurd. Wacht even en probeer het opnieuw.",
                Errors = []
            });
            return;
        }

        await context.HttpContext.Response.WriteAsync("Er zijn in korte tijd te veel verzoeken verstuurd. Wacht even en probeer het opnieuw.");
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "onbekend";
        var partitionKey = userId is not null ? $"user:{userId}" : $"ip:{remoteIp}";
        var isMutationRequest =
            HttpMethods.IsPost(httpContext.Request.Method)
            || HttpMethods.IsPut(httpContext.Request.Method)
            || HttpMethods.IsPatch(httpContext.Request.Method)
            || HttpMethods.IsDelete(httpContext.Request.Method);
        var path = httpContext.Request.Path;

        if (!isMutationRequest)
        {
            return RateLimitPartition.GetSlidingWindowLimiter(
                $"read:{partitionKey}",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 600,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        if (HttpMethods.IsPost(httpContext.Request.Method) && path.Equals(loginPath, StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetSlidingWindowLimiter(
                $"login:{remoteIp}",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(10),
                    SegmentsPerWindow = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        if (HttpMethods.IsPost(httpContext.Request.Method) && path.Equals(createTenderPath, StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetSlidingWindowLimiter(
                $"tender-create:{partitionKey}",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 25,
                    Window = TimeSpan.FromMinutes(10),
                    SegmentsPerWindow = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

// Seed development data
if (app.Environment.IsDevelopment())
{
    await DbSeeder.SeedDevelopmentDataAsync(app.Services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (AntiforgeryValidationException) when (context.Request.Path.StartsWithSegments("/api"))
    {
        if (context.Response.HasStarted)
            throw;

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new ApiResponse<object?>
        {
            Data = null,
            Message = "Uw formulier kon niet veilig worden verwerkt. Vernieuw de pagina en probeer het opnieuw.",
            Errors = []
        });
    }
    catch (Exception ex) when (context.Request.Path.StartsWithSegments("/api"))
    {
        if (context.Response.HasStarted)
            throw;

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new ApiResponse<object?>
        {
            Data = null,
            Message = app.Environment.IsDevelopment()
                ? $"Er ging iets mis op de server: {ex.Message}"
                : "Er ging iets mis op de server. Probeer het later opnieuw.",
            Errors = []
        });
    }
});

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static async Task TryLogAccessDeniedAsync(HttpContext httpContext, SecurityAuditOutcome outcome)
{
    var logger = httpContext.RequestServices
        .GetService<ILoggerFactory>()
        ?.CreateLogger("SecurityAudit");

    try
    {
        var securityAuditService = httpContext.RequestServices.GetRequiredService<ISecurityAuditService>();

        await securityAuditService.TryLogAsync(new SecurityAuditEvent
        {
            EventType = SecurityAuditEventType.AccessDenied,
            Outcome = outcome,
            ActorUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            TraceId = httpContext.TraceIdentifier,
            Details = new Dictionary<string, string>
            {
                ["method"] = httpContext.Request.Method,
                ["path"] = httpContext.Request.Path.Value ?? string.Empty
            }
        });
    }
    catch (Exception ex)
    {
        logger?.LogError(ex, "Security audit service could not be resolved while handling access denied.");
    }
}
