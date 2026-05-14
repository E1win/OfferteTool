namespace Tests.E2E.TestData;

public static class TestUsers
{
    public static readonly TestUser Inkoper = new("inkoper@test.nl", "Password123!");
}

public sealed record TestUser(string Email, string Password);
