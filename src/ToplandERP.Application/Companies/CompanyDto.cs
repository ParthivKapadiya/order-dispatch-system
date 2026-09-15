namespace ToplandERP.Application.Companies;

public sealed class CompanyDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string? Address { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public bool IsActive { get; init; }
}
