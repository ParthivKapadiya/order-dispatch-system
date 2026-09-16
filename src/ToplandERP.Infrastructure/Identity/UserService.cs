using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Security;
using ToplandERP.Application.Users;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Security;
using ToplandERP.Infrastructure.Data;

namespace ToplandERP.Infrastructure.Identity;

public sealed class UserService : IUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<ResetUserPasswordRequest> _passwordValidator;

    public UserService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<ResetUserPasswordRequest> passwordValidator)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _passwordValidator = passwordValidator;
    }

    public IReadOnlyList<string> GetAssignableRoles()
    {
        EnsureCanManageUsers();
        return UserManagementRules.AssignableRoles(_currentUser.IsSuperAdmin, IsCompanyAdmin);
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();
        var page = CompanyScope.NormalizePage(query.Page);
        var pageSize = CompanyScope.NormalizePageSize(query.PageSize);

        var users = _dbContext.Users.AsNoTracking().AsQueryable();
        users = ApplyCompanyScope(users, query.CompanyId);
        users = ExcludeSuperAdmins(users);

        if (query.IsActive.HasValue)
        {
            users = users.Where(user => user.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            users = users.Where(user =>
                user.FullName.Contains(term)
                || user.EmployeeCode.Contains(term)
                || (user.UserName != null && user.UserName.Contains(term))
                || (user.Email != null && user.Email.Contains(term))
                || (user.PhoneNumber != null && user.PhoneNumber.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = await _dbContext.Roles.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Name == query.Role, cancellationToken);
            if (role is null)
            {
                return Empty(page, pageSize);
            }

            users = from user in users
                    join userRole in _dbContext.UserRoles on user.Id equals userRole.UserId
                    where userRole.RoleId == role.Id
                    select user;
        }

        var total = await users.CountAsync(cancellationToken);
        var pageItems = await users
            .OrderBy(user => user.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserDto>(pageItems.Count);
        foreach (var user in pageItems)
        {
            dtos.Add(await ToDtoAsync(user, cancellationToken));
        }

        return new PagedResult<UserDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();
        var user = await _userManager.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null || !CanManageTarget(user.CompanyId))
        {
            return null;
        }

        return await ToDtoAsync(user, cancellationToken);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!UserManagementRules.CanAssignRole(_currentUser.IsSuperAdmin, IsCompanyAdmin, request.Role))
        {
            throw new ForbiddenException("You are not allowed to assign that role.");
        }

        var companyId = ResolveCompanyId(request.CompanyId);
        await EnsureCompanyExistsAsync(companyId, cancellationToken);
        await EnsureUniqueEmployeeCodeAsync(companyId, request.EmployeeCode, null, cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            EmployeeCode = request.EmployeeCode.Trim(),
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.Mobile?.Trim(),
            CompanyId = companyId,
            EmailConfirmed = true,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.TemporaryPassword);
        EnsureSucceeded(createResult, "Unable to create the user.");

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            EnsureSucceeded(roleResult, "Unable to assign the selected role.");
        }

        _auditLogger.Record("UserCreated", "ApplicationUser", user.Id, companyId, $"{user.UserName}:{request.Role}");
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(user.Id, cancellationToken))!;
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await GetManagedUserAsync(id);

        if (!UserManagementRules.CanAssignRole(_currentUser.IsSuperAdmin, IsCompanyAdmin, request.Role))
        {
            throw new ForbiddenException("You are not allowed to assign that role.");
        }

        await EnsureUniqueEmployeeCodeAsync(user.CompanyId, request.EmployeeCode, user.Id, cancellationToken);

        var previousRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        user.FullName = request.FullName.Trim();
        user.EmployeeCode = request.EmployeeCode.Trim();
        user.Email = request.Email.Trim();
        user.PhoneNumber = request.Mobile?.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        EnsureSucceeded(updateResult, "Unable to update the user.");

        if (!string.Equals(previousRole, request.Role, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(previousRole))
            {
                EnsureSucceeded(await _userManager.RemoveFromRoleAsync(user, previousRole), "Unable to update the user role.");
            }

            EnsureSucceeded(await _userManager.AddToRoleAsync(user, request.Role), "Unable to update the user role.");
            _auditLogger.Record("UserRoleChanged", "ApplicationUser", user.Id, user.CompanyId, $"{previousRole}->{request.Role}");
        }
        else
        {
            _auditLogger.Record("UserUpdated", "ApplicationUser", user.Id, user.CompanyId, user.UserName);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(user.Id, cancellationToken))!;
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();
        var user = await GetManagedUserAsync(id);
        if (user.Id == _currentUser.UserId)
        {
            throw new BusinessException("You cannot change the status of your own account.");
        }

        user.IsActive = isActive;
        EnsureSucceeded(await _userManager.UpdateAsync(user), "Unable to update user status.");
        if (!isActive)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        _auditLogger.Record(isActive ? "UserActivated" : "UserDeactivated", "ApplicationUser", user.Id, user.CompanyId, user.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();
        await _passwordValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await GetManagedUserAsync(id);
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        EnsureSucceeded(await _userManager.ResetPasswordAsync(user, token, request.NewPassword), "Unable to reset the password.");
        await _userManager.UpdateSecurityStampAsync(user);
        _auditLogger.Record("UserPasswordReset", "ApplicationUser", user.Id, user.CompanyId, user.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private bool IsCompanyAdmin =>
        !_currentUser.IsSuperAdmin && _currentUser.Roles.Contains(RoleNames.CompanyAdmin);

    private void EnsureCanManageUsers()
    {
        if (_currentUser.IsSuperAdmin || IsCompanyAdmin)
        {
            return;
        }

        throw new ForbiddenException("You are not allowed to manage users.");
    }

    private Guid ResolveCompanyId(Guid? requestedCompanyId)
    {
        if (_currentUser.IsSuperAdmin)
        {
            if (!requestedCompanyId.HasValue || requestedCompanyId.Value == Guid.Empty)
            {
                throw new BusinessException("Company is required.");
            }

            return requestedCompanyId.Value;
        }

        if (!_currentUser.CompanyId.HasValue)
        {
            throw new ForbiddenException("Your account is not assigned to a company.");
        }

        return _currentUser.CompanyId.Value;
    }

    private bool CanManageTarget(Guid? targetCompanyId)
    {
        return UserManagementRules.CanManageCompanyUsers(_currentUser.IsSuperAdmin, _currentUser.CompanyId, targetCompanyId);
    }

    private IQueryable<ApplicationUser> ApplyCompanyScope(IQueryable<ApplicationUser> users, Guid? filterCompanyId)
    {
        if (_currentUser.IsSuperAdmin)
        {
            if (filterCompanyId.HasValue && filterCompanyId.Value != Guid.Empty)
            {
                return users.Where(user => user.CompanyId == filterCompanyId.Value);
            }

            return users;
        }

        return users.Where(user => user.CompanyId == _currentUser.CompanyId);
    }

    private IQueryable<ApplicationUser> ExcludeSuperAdmins(IQueryable<ApplicationUser> users)
    {
        var superAdminRoleId = _dbContext.Roles
            .Where(role => role.Name == RoleNames.SuperAdmin)
            .Select(role => role.Id);

        var superAdminUserIds = _dbContext.UserRoles
            .Where(userRole => superAdminRoleId.Contains(userRole.RoleId))
            .Select(userRole => userRole.UserId);

        return users.Where(user => !superAdminUserIds.Contains(user.Id));
    }

    private async Task<ApplicationUser> GetManagedUserAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException("User was not found.");

        if (!CanManageTarget(user.CompanyId))
        {
            throw new ForbiddenException("You cannot manage users from another company.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(RoleNames.SuperAdmin) && !_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenException("You cannot manage this user.");
        }

        if (roles.Contains(RoleNames.SuperAdmin) && _currentUser.IsSuperAdmin)
        {
            throw new ForbiddenException("SuperAdmin accounts are not managed from this screen.");
        }

        return user;
    }

    private async Task EnsureCompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Companies.AnyAsync(company => company.Id == companyId && company.IsActive, cancellationToken);
        if (!exists)
        {
            throw new BusinessException("The selected company is not available.");
        }
    }

    private async Task EnsureUniqueEmployeeCodeAsync(Guid? companyId, string employeeCode, Guid? excludeId, CancellationToken cancellationToken)
    {
        var code = employeeCode.Trim();
        var duplicate = await _dbContext.Users.AnyAsync(
            user => user.EmployeeCode == code
                && user.CompanyId == companyId
                && (!excludeId.HasValue || user.Id != excludeId.Value),
            cancellationToken);
        if (duplicate)
        {
            throw new BusinessException("An employee with this code already exists in the company.");
        }
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        var company = user.CompanyId.HasValue
            ? await _dbContext.Companies.AsNoTracking().FirstOrDefaultAsync(item => item.Id == user.CompanyId.Value, cancellationToken)
            : null;

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            EmployeeCode = user.EmployeeCode,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Mobile = user.PhoneNumber,
            CompanyId = user.CompanyId,
            CompanyName = company?.Name ?? string.Empty,
            CompanyCode = company?.Code ?? string.Empty,
            Role = role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    private static PagedResult<UserDto> Empty(int page, int pageSize)
    {
        return new PagedResult<UserDto>
        {
            Items = [],
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
    }

    private static void EnsureSucceeded(IdentityResult result, string fallback)
    {
        if (result.Succeeded)
        {
            return;
        }

        var message = string.Join(" ", result.Errors.Select(error => error.Description));
        throw new BusinessException(string.IsNullOrWhiteSpace(message) ? fallback : message);
    }
}
