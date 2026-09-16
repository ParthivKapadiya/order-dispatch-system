namespace ToplandERP.Application.Abstractions;

public sealed record DirectoryUser(Guid UserId, Guid? CompanyId, string FullName, bool IsActive);

public interface IUserDirectory
{
    Task<IReadOnlyList<DirectoryUser>> GetActiveUsersInRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default);
}
