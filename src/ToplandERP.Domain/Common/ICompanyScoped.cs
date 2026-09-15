namespace ToplandERP.Domain.Common;

/// <summary>
/// Marks a record as belonging to a single company. Company isolation is enforced
/// in the application and data layers, never from browser-supplied identifiers alone.
/// </summary>
public interface ICompanyScoped
{
    Guid CompanyId { get; }
}
