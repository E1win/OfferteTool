using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedDevelopmentDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedUserAsync(userManager, "inkoper@test.nl", "Password123!", "Inkoper", "Jan", "de Vries");
        await SeedUserAsync(userManager, "beoordelaar@test.nl", "Password123!", "Beoordelaar", "Pieter", "Bakker");
        await SeedUserAsync(userManager, "beheerder@test.nl", "Password123!", "Beheerder", "Anna", "Jansen");
        await SeedUserAsync(userManager, "leverancier@test.nl", "Password123!", "Leverancier", "Maria", "Visser");
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role,
        string firstName,
        string lastName)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName
        };

        var result = await userManager.CreateAsync(user, password);

        if (! result.Succeeded)
            throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        var addRoleResult = await userManager.AddToRoleAsync(user, role);

        if (! addRoleResult.Succeeded)
            throw new Exception($"Failed to assign user to role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}
