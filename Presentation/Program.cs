using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("De applicatie is niet goed geconfigureerd.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddScoped<ITenderRepository, TenderRepository>();
builder.Services.AddScoped<ITenderQuestionRepository, TenderQuestionRepository>();

builder.Services.AddScoped<ITenderService, TenderService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITenderQuestionService, TenderQuestionService>();

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Voer een geldige waarde in.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(_ => "Vul een geldig getal in.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Dit veld is verplicht.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, fieldName) => $"{fieldName} bevat geen geldige waarde.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(fieldName => $"{fieldName} is verplicht.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "Deze waarde ontbreekt.");
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
