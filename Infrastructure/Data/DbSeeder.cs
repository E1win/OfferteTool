using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedDevelopmentDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await SeedUserAsync(userManager, "inkoper@test.nl", "Password123!", "Inkoper");
        await SeedUserAsync(userManager, "beoordelaar@test.nl", "Password123!", "Beoordelaar");
        await SeedUserAsync(userManager, "beheerder@test.nl", "Password123!", "Beheerder");
        await SeedUserAsync(userManager, "leverancier@test.nl", "Password123!", "Leverancier");
    }

    private static async Task SeedUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (! result.Succeeded)
            throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        var addRoleResult = await userManager.AddToRoleAsync(user, role);

        if (! addRoleResult.Succeeded)
            throw new Exception($"Failed to assign user to role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}
