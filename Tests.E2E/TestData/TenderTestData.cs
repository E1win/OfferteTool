namespace Tests.E2E.TestData;

public sealed record TenderDraft(string Title, string Description, DateOnly EndDate, bool IsPublic);

public static class TenderTestData
{
    public static TenderDraft CreateDraft(string scenarioName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

        return new TenderDraft(
            Title: $"E2E offertetraject {scenarioName} {timestamp}",
            Description: $"Automatisch aangemaakt door E2E-test {scenarioName}.",
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            IsPublic: true);
    }
}
