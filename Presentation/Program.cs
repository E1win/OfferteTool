using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Presentation.Models.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("De applicatie is niet goed geconfigureerd.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new ApiResponse<object?>
        {
            Data = null,
            Message = "Uw sessie is verlopen. Meld u opnieuw aan en probeer het daarna nog eens.",
            Errors = []
        });
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new ApiResponse<object?>
        {
            Data = null,
            Message = "U heeft geen toestemming om deze actie uit te voeren.",
            Errors = []
        });
    };
});

builder.Services.AddScoped<ITenderRepository, TenderRepository>();
builder.Services.AddScoped<ITenderQuestionRepository, TenderQuestionRepository>();

builder.Services.AddScoped<ITenderService, TenderService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITenderQuestionService, TenderQuestionService>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Voer een geldige waarde in.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(_ => "Vul een geldig getal in.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Dit veld is verplicht.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, fieldName) => $"{fieldName} bevat geen geldige waarde.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(fieldName => $"{fieldName} is verplicht.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "Deze waarde ontbreekt.");
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
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
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
