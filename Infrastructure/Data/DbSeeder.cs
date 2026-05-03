using Domain.Constants;
using Domain.Entities;
using Domain.Entities.TenderQuestions;
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
        var secondSupplierOrg = await SeedOrganisationAsync(dbContext,
            "supplier-two-id", "Leverancier 2 B.V.", "87654322", OrganisationType.Supplier);
        var thirdSupplierOrg = await SeedOrganisationAsync(dbContext,
            "supplier-three-id", "Leverancier 3 B.V.", "87654323", OrganisationType.Supplier);

        await SeedUserAsync(userManager, "inkoper@test.nl", "Password123!", Roles.Inkoper, "Jan", "de Vries", clientOrg.Id);
        await SeedUserAsync(userManager, "beoordelaar@test.nl", "Password123!", Roles.Beoordelaar, "Pieter", "Bakker", clientOrg.Id);
        await SeedUserAsync(userManager, "beheerder@test.nl", "Password123!", Roles.Beheerder, "Anna", "Jansen");
        await SeedUserAsync(userManager, "leverancier@test.nl", "Password123!", Roles.Leverancier, "Maria", "Visser", supplierOrg.Id);
        await SeedUserAsync(userManager, "leverancier2@test.nl", "Password123!", Roles.Leverancier, "Sanne", "de Boer", secondSupplierOrg.Id);
        await SeedUserAsync(userManager, "leverancier3@test.nl", "Password123!", Roles.Leverancier, "Daan", "Meijer", thirdSupplierOrg.Id);

        var furnitureTender = await SeedTenderAsync(dbContext, "tender-one-id", "Kantoormeubelen 2026",
            "Levering van ergonomische bureaustoelen en sta-bureaus voor 200 werkplekken.",
            new DateOnly(2026, 6, 15), TenderStatus.Open, true, clientOrg.Id);

        var infrastructureTender = await SeedTenderAsync(dbContext, "tender-two-id", "IT Infrastructuur Upgrade",
            "Vervanging van netwerkapparatuur en servers in drie datacenters.",
            new DateOnly(2026, 8, 31), TenderStatus.Design, true, clientOrg.Id);

        var cleaningTender = await SeedTenderAsync(dbContext, "tender-three-id", "Schoonmaakdiensten",
            "Dagelijkse schoonmaak voor het hoofdkantoor.",
            new DateOnly(2026, 7, 31), TenderStatus.Open, true, clientOrg.Id);

        var cateringTender = await SeedTenderAsync(dbContext, "tender-four-id", "Bedrijfscatering",
            "Lunchvoorziening en vergaderservice voor medewerkers.",
            new DateOnly(2026, 9, 30), TenderStatus.Design, true, clientOrg.Id);

        var securityTender = await SeedTenderAsync(dbContext, "tender-five-id", "Beveiligingsdiensten",
            "Avond- en weekendbeveiliging voor twee locaties.",
            new DateOnly(2026, 10, 15), TenderStatus.Design, false, clientOrg.Id);

        await SeedTenderQuestionsAsync(dbContext, furnitureTender.Id,
        [
            CreateTextQuestion("tender-one-question-one", furnitureTender.Id, 0, "Beschrijf de ergonomische eigenschappen van de stoelen.", 30, rows: 4, maxLength: 1000),
            CreateNumberQuestion("tender-one-question-two", furnitureTender.Id, 1, "Wat is de levertijd in werkdagen?", null, minValue: 1, maxValue: 90),
            CreateChoiceQuestion("tender-one-question-three", furnitureTender.Id, 2, "Welke garantieperiode biedt u?", 25, false,
            [
                "Minder dan 2 jaar",
                "2 tot 5 jaar",
                "Meer dan 5 jaar"
            ]),
            CreateTextQuestion("tender-one-question-four", furnitureTender.Id, 3, "Beschrijf uw service- en onderhoudsaanpak.", 25, rows: 3, maxLength: 800)
        ]);

        await SeedTenderQuestionsAsync(dbContext, infrastructureTender.Id,
        [
            CreateTextQuestion("tender-two-question-one", infrastructureTender.Id, 0, "Beschrijf de voorgestelde migratieaanpak.", 35, rows: 5, maxLength: 1200),
            CreateChoiceQuestion("tender-two-question-two", infrastructureTender.Id, 1, "Welke supportvormen zijn inbegrepen?", 25, true,
            [
                "Kantooruren",
                "24/7 monitoring",
                "On-site support"
            ]),
            CreateNumberQuestion("tender-two-question-three", infrastructureTender.Id, 2, "Wat is de verwachte doorlooptijd in weken?", 20, minValue: 1, maxValue: 52),
            CreateTextQuestion("tender-two-question-four", infrastructureTender.Id, 3, "Welke beveiligingsmaatregelen neemt u tijdens de overgang?", null, rows: 4, maxLength: 1000)
        ]);

        await SeedTenderQuestionsAsync(dbContext, cleaningTender.Id,
        [
            CreateChoiceQuestion("tender-three-question-one", cleaningTender.Id, 0, "Welke duurzame schoonmaakmiddelen gebruikt u?", 30, true,
            [
                "EU Ecolabel",
                "Biologisch afbreekbaar",
                "Navulverpakkingen"
            ]),
            CreateNumberQuestion("tender-three-question-two", cleaningTender.Id, 1, "Hoeveel vaste medewerkers plant u in?", null, minValue: 1, maxValue: 20),
            CreateTextQuestion("tender-three-question-three", cleaningTender.Id, 2, "Beschrijf hoe u kwaliteitscontrole uitvoert.", 45, rows: 4, maxLength: 900)
        ]);

        await SeedTenderQuestionsAsync(dbContext, cateringTender.Id,
        [
            CreateTextQuestion("tender-four-question-one", cateringTender.Id, 0, "Geef een voorbeeldmenu voor een normale werkweek.", null, rows: 5, maxLength: 1200),
            CreateNumberQuestion("tender-four-question-two", cateringTender.Id, 1, "Wat is de prijs per lunch per medewerker?", 30, minValue: 1, maxValue: 25),
            CreateChoiceQuestion("tender-four-question-three", cateringTender.Id, 2, "Welke dieetwensen ondersteunt u standaard?", 25, true,
            [
                "Vegetarisch",
                "Veganistisch",
                "Glutenvrij",
                "Halal"
            ]),
            CreateTextQuestion("tender-four-question-four", cateringTender.Id, 3, "Beschrijf uw aanpak voor voedselverspilling.", 20, rows: 3, maxLength: 800)
        ]);

        await SeedTenderQuestionsAsync(dbContext, securityTender.Id,
        [
            CreateChoiceQuestion("tender-five-question-one", securityTender.Id, 0, "Welke certificeringen bezit uw organisatie?", null, true,
            [
                "ISO 9001",
                "VCA",
                "Beveiligingskeurmerk"
            ]),
            CreateTextQuestion("tender-five-question-two", securityTender.Id, 1, "Beschrijf de procedure bij incidenten.", 35, rows: 4, maxLength: 1000),
            CreateNumberQuestion("tender-five-question-three", securityTender.Id, 2, "Binnen hoeveel minuten kan een surveillant ter plaatse zijn?", 35, minValue: 1, maxValue: 120)
        ]);
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

    private static async Task<Tender> SeedTenderAsync(
        AppDbContext dbContext,
        string idSeed,
        string title,
        string description,
        DateOnly endDate,
        TenderStatus status,
        bool isPublic,
        Guid organisationId)
    {
        var id = GuidFromSeed(idSeed);
        var existing = await dbContext.Tenders.FindAsync(id);
        if (existing is not null)
            return existing;

        var tender = new Tender
        {
            Id = id,
            Title = title,
            Description = description,
            EndDate = endDate,
            Status = status,
            IsPublic = isPublic,
            OrganisationId = organisationId
        };

        dbContext.Tenders.Add(tender);
        await dbContext.SaveChangesAsync();
        return tender;
    }

    private static async Task SeedTenderQuestionsAsync(
        AppDbContext dbContext,
        Guid tenderId,
        IEnumerable<TenderQuestion> questions)
    {
        if (await dbContext.TenderQuestions.AnyAsync(question => question.TenderId == tenderId))
            return;

        dbContext.TenderQuestions.AddRange(questions);
        await dbContext.SaveChangesAsync();
    }

    private static TextQuestion CreateTextQuestion(
        string idSeed,
        Guid tenderId,
        int order,
        string text,
        int? score,
        int rows,
        int maxLength)
    {
        return new TextQuestion
        {
            Id = GuidFromSeed(idSeed),
            TenderId = tenderId,
            Order = order,
            Text = text,
            Score = score,
            Rows = rows,
            MaxLength = maxLength
        };
    }

    private static NumberQuestion CreateNumberQuestion(
        string idSeed,
        Guid tenderId,
        int order,
        string text,
        int? score,
        decimal minValue,
        decimal maxValue)
    {
        return new NumberQuestion
        {
            Id = GuidFromSeed(idSeed),
            TenderId = tenderId,
            Order = order,
            Text = text,
            Score = score,
            MinValue = minValue,
            MaxValue = maxValue
        };
    }

    private static ChoiceQuestion CreateChoiceQuestion(
        string idSeed,
        Guid tenderId,
        int order,
        string text,
        int? score,
        bool allowMultipleSelection,
        IReadOnlyList<string> options)
    {
        var questionId = GuidFromSeed(idSeed);

        return new ChoiceQuestion
        {
            Id = questionId,
            TenderId = tenderId,
            Order = order,
            Text = text,
            Score = score,
            AllowMultipleSelection = allowMultipleSelection,
            Options = options
                .Select((option, optionOrder) => new TenderQuestionOption
                {
                    Id = GuidFromSeed($"{idSeed}-option-{optionOrder}"),
                    QuestionId = questionId,
                    Order = optionOrder,
                    Text = option
                })
                .ToList()
        };
    }
}
