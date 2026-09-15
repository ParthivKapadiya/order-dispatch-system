namespace ToplandERP.Infrastructure.Options;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public SuperAdminSeedOptions SuperAdmin { get; set; } = new();
}

public sealed class SuperAdminSeedOptions
{
    public string UserName { get; set; } = "admin";

    public string Email { get; set; } = "admin@localhost";

    public string FullName { get; set; } = "System Administrator";

    public string? Password { get; set; }
}
