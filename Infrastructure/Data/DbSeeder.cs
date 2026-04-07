using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedDevelopmentDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var clientOrg = await SeedOrganisationAsync(dbContext,
            "acme-client-id", "Opdrachtgever B.V.", "12345678", OrganisationType.Client);

        var supplierOrg = await SeedOrganisationAsync(dbContext,
            "supplier-one-id", "Leverancier B.V.", "87654321", OrganisationType.Supplier);

        await SeedUserAsync(userManager, "inkoper@test.nl", "Password123!", "Inkoper", "Jan", "de Vries", clientOrg.Id);
        await SeedUserAsync(userManager, "beoordelaar@test.nl", "Password123!", "Beoordelaar", "Pieter", "Bakker", clientOrg.Id);
        await SeedUserAsync(userManager, "beheerder@test.nl", "Password123!", "Beheerder", "Anna", "Jansen");
        await SeedUserAsync(userManager, "leverancier@test.nl", "Password123!", "Leverancier", "Maria", "Visser", supplierOrg.Id);
    }

    private static async Task<Organisation> SeedOrganisationAsync(
        AppDbContext dbContext,
        string idSeed,
        string name,
        string kvkNumber,
        OrganisationType type)
    {
        var id = GuidFromSeed(idSeed);
        var existing = await dbContext.Organisations.FindAsync(id);
        if (existing is not null)
            return existing;

        var organisation = new Organisation
        {
            Id = id,
            Name = name,
            KvkNumber = kvkNumber,
            OrganisationType = type
        };

        dbContext.Organisations.Add(organisation);
        await dbContext.SaveChangesAsync();
        return organisation;
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role,
        string firstName,
        string lastName,
        Guid? organisationId = null)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            OrganisationId = organisationId
        };

        var result = await userManager.CreateAsync(user, password);

        if (! result.Succeeded)
            throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        var addRoleResult = await userManager.AddToRoleAsync(user, role);

        if (! addRoleResult.Succeeded)
            throw new Exception($"Failed to assign user to role: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
    }

    private static Guid GuidFromSeed(string seed) =>
        new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(seed)));
}
