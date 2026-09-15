namespace ToplandERP.Domain.Constants;

/// <summary>
/// Stable identifiers used only for development/production seed data.
/// Business logic must look up companies by code or authenticated context, not these values.
/// </summary>
public static class SeedIdentifiers
{
    public static readonly Guid GravisCompanyId = Guid.Parse("7c8e3d21-4a9f-4b6e-9d12-0f3a8c1b5e70");
    public static readonly Guid JeekoCompanyId = Guid.Parse("2f6b9a44-8c11-4e37-b5d0-91a47e2c6f18");
    public static readonly Guid ShreeCompanyId = Guid.Parse("9d4e1c88-0b72-4f9a-a3e6-5c8d2b17f4a0");
}
