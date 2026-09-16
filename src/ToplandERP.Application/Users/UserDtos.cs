namespace ToplandERP.Application.Users;

public sealed class UserListQuery
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CompanyId { get; set; }

    public string? Role { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public sealed class UserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string EmployeeCode { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Mobile { get; init; }
    public Guid? CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public Guid? CompanyId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
}

public sealed class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string Role { get; set; } = string.Empty;
}

public sealed class ResetUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public interface IUserService
{
    Task<ToplandERP.Application.Common.PagedResult<UserDto>> GetPagedAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<string> GetAssignableRoles();
}
