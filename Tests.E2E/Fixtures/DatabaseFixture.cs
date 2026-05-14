using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tests.E2E.Configuration;

namespace Tests.E2E.Fixtures;

public sealed class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; } = GetConnectionString();

    public async Task InitializeAsync()
    {
        ThrowIfConnectionStringDoesNotLookSafe(ConnectionString);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning);
            })
            .Options;

        await using var dbContext = new AppDbContext(options);

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        await SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString);
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning);
            });
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.Configure<PasswordHasherOptions>(options =>
        {
            options.IterationCount = 600_000;
        });

        using var serviceProvider = services.BuildServiceProvider();

        await DbSeeder.SeedDevelopmentDataAsync(serviceProvider);
    }

    private static string GetConnectionString()
    {
        E2ERuntimeEnvironment.Load();

        var connectionString = Environment.GetEnvironmentVariable(E2ERuntimeEnvironment.DefaultConnectionKey);

        if (!string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        throw new InvalidOperationException(
            $"Set {E2ERuntimeEnvironment.DefaultConnectionKey} in {E2ERuntimeEnvironment.LocalFileName} or as an environment variable.");
    }

    private static void ThrowIfConnectionStringDoesNotLookSafe(string connectionString)
    {
        var databaseName = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .Where(parts => parts[0].Equals("Database", StringComparison.OrdinalIgnoreCase))
            .Select(parts => parts[1])
            .SingleOrDefault();

        if (databaseName is null || !databaseName.Contains("E2E", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The E2E database name must contain 'E2E' because DatabaseFixture deletes and recreates the database.");
        }
    }
}
