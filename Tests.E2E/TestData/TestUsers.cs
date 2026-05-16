namespace Tests.E2E.TestData;

public static class TestUsers
{
    public static readonly TestUser Inkoper = new("inkoper@test.nl", "Password123!");
    public static readonly TestUser Beoordelaar = new("beoordelaar@test.nl", "Password123!");
    public static readonly TestUser Leverancier = new("leverancier@test.nl", "Password123!");
}

public sealed record TestUser(string Email, string Password);
