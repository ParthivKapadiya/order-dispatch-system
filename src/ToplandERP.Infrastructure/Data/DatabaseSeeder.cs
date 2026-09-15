using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.Infrastructure.Options;

namespace ToplandERP.Infrastructure.Data;

public sealed class DatabaseSeeder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedOptions _seedOptions;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext dbContext,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<SeedOptions> seedOptions,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
        _seedOptions = seedOptions.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseCreatedAsync(cancellationToken);
        await SeedCompaniesAsync(cancellationToken);
        await SeedRolesAsync();
        await SeedSuperAdminAsync();
    }

    private async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.Database.IsSqlServer())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private async Task SeedCompaniesAsync(CancellationToken cancellationToken)
    {
        var companies = new[]
        {
            new Company
            {
                Id = SeedIdentifiers.GravisCompanyId,
                Name = "Gravis India Private Limited",
                Code = CompanyCodes.Gravis,
                IsActive = true
            },
            new Company
            {
                Id = SeedIdentifiers.JeekoCompanyId,
                Name = "Jeeko Agritech LLP",
                Code = CompanyCodes.Jeeko,
                IsActive = true
            },
            new Company
            {
                Id = SeedIdentifiers.ShreeCompanyId,
                Name = "Shree Agency",
                Code = CompanyCodes.Shree,
                IsActive = true
            }
        };

        foreach (var company in companies)
        {
            var exists = await _dbContext.Companies
                .IgnoreQueryFilters()
                .AnyAsync(item => item.Code == company.Code, cancellationToken);

            if (exists)
            {
                continue;
            }

            _dbContext.Companies.Add(company);
            _logger.LogInformation("Seeded company {CompanyCode} ({CompanyName})", company.Code, company.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in RoleNames.All)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await _roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed role '{roleName}': {FormatIdentityErrors(result)}");
            }

            _logger.LogInformation("Seeded role {RoleName}", roleName);
        }
    }

    private async Task SeedSuperAdminAsync()
    {
        var options = _seedOptions.SuperAdmin;
        var userName = string.IsNullOrWhiteSpace(options.UserName) ? "admin" : options.UserName.Trim();
        var existing = await _userManager.FindByNameAsync(userName);

        if (existing is not null)
        {
            if (!await _userManager.IsInRoleAsync(existing, RoleNames.SuperAdmin))
            {
                await _userManager.AddToRoleAsync(existing, RoleNames.SuperAdmin);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            _logger.LogWarning(
                "SuperAdmin user '{UserName}' was not created because Seed:SuperAdmin:Password is not configured. Set it with user secrets or an environment variable.",
                userName);
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = options.Email,
            EmailConfirmed = true,
            FullName = string.IsNullOrWhiteSpace(options.FullName) ? "System Administrator" : options.FullName,
            CompanyId = null,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(admin, options.Password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to seed SuperAdmin: {FormatIdentityErrors(createResult)}");
        }

        var roleResult = await _userManager.AddToRoleAsync(admin, RoleNames.SuperAdmin);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to assign SuperAdmin role: {FormatIdentityErrors(roleResult)}");
        }

        _logger.LogInformation("Seeded SuperAdmin user {UserName}", userName);
    }

    private static string FormatIdentityErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }
}
