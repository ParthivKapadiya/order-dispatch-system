using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ToplandERP.Application.Common;
using ToplandERP.Application.Users;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;

namespace ToplandERP.UnitTests.Application;

public class UserServiceTests
{
    [Fact]
    public async Task CompanyAdmin_cannot_create_SuperAdmin()
    {
        var (service, _) = await CreateAsync(
            new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.CompanyAdmin] });

        var act = async () => await service.CreateAsync(ValidRequest(RoleNames.SuperAdmin, SeedIdentifiers.JeekoCompanyId));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CompanyAdmin_cannot_create_user_in_another_company()
    {
        var (service, dbContext) = await CreateAsync(
            new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.CompanyAdmin] });

        var created = await service.CreateAsync(ValidRequest(RoleNames.SalesEmployee, SeedIdentifiers.JeekoCompanyId));

        created.CompanyId.Should().Be(SeedIdentifiers.GravisCompanyId);
        (await dbContext.Users.CountAsync(user => user.CompanyId == SeedIdentifiers.JeekoCompanyId)).Should().Be(0);
    }

    [Fact]
    public async Task CompanyAdmin_cannot_access_another_company_users()
    {
        var otherUserId = Guid.NewGuid();
        var (service, _) = await CreateAsync(
            new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.CompanyAdmin] },
            async (db, users) =>
            {
                var other = new ApplicationUser
                {
                    Id = otherUserId,
                    UserName = "jeeko.admin",
                    Email = "jeeko.admin@example.com",
                    FullName = "Jeeko Admin",
                    EmployeeCode = "JEE-ADM",
                    CompanyId = SeedIdentifiers.JeekoCompanyId,
                    EmailConfirmed = true,
                    IsActive = true
                };
                await users.CreateAsync(other, "DevP@ssw0rd!123");
                await users.AddToRoleAsync(other, RoleNames.CompanyAdmin);
            });

        (await service.GetByIdAsync(otherUserId)).Should().BeNull();
        var list = await service.GetPagedAsync(new UserListQuery());
        list.Items.Should().NotContain(item => item.UserName == "jeeko.admin");
    }

    [Fact]
    public async Task SuperAdmin_can_create_users_across_companies()
    {
        var (service, _) = await CreateAsync(new TestCurrentUser { Roles = [RoleNames.SuperAdmin] });

        var created = await service.CreateAsync(ValidRequest(RoleNames.CompanyAdmin, SeedIdentifiers.ShreeCompanyId, "shree.admin", "SHREE-ADM", "shree.admin@example.com"));

        created.CompanyId.Should().Be(SeedIdentifiers.ShreeCompanyId);
        created.Role.Should().Be(RoleNames.CompanyAdmin);
    }

    private static CreateUserRequest ValidRequest(string role, Guid companyId, string userName = "gra.sales", string code = "GRA-S1", string email = "gra.sales@example.com")
    {
        return new CreateUserRequest
        {
            FullName = "Test Employee",
            EmployeeCode = code,
            UserName = userName,
            Email = email,
            Mobile = "9876543210",
            CompanyId = companyId,
            Role = role,
            TemporaryPassword = "DevP@ssw0rd!123"
        };
    }

    private static async Task<(UserService Service, ApplicationDbContext Db)> CreateAsync(
        TestCurrentUser currentUser,
        Func<ApplicationDbContext, UserManager<ApplicationUser>, Task>? extra = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(options, SystemCurrentUser.Instance);
        dbContext.Companies.AddRange(
            new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true },
            new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko", Code = CompanyCodes.Jeeko, IsActive = true },
            new Company { Id = SeedIdentifiers.ShreeCompanyId, Name = "Shree", Code = CompanyCodes.Shree, IsActive = true });
        await dbContext.SaveChangesAsync();

        var users = CreateUserManager(dbContext);
        var roles = CreateRoleManager(dbContext);
        foreach (var roleName in RoleNames.All)
        {
            await roles.CreateAsync(new IdentityRole<Guid> { Name = roleName });
        }

        if (extra is not null)
        {
            await extra(dbContext, users);
        }

        var scopedDb = new ApplicationDbContext(options, currentUser);
        var scopedUsers = CreateUserManager(scopedDb);
        var service = new UserService(
            scopedDb,
            scopedUsers,
            currentUser,
            new TestAuditLogger(),
            new CreateUserRequestValidator(),
            new UpdateUserRequestValidator(),
            new ResetUserPasswordRequestValidator());

        return (service, scopedDb);
    }

    private static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext dbContext)
    {
        var store = new UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>(dbContext);
        var options = Options.Create(new IdentityOptions
        {
            Password =
            {
                RequiredLength = 8,
                RequireDigit = true,
                RequireNonAlphanumeric = true,
                RequireUppercase = true,
                RequireLowercase = true
            },
            User = { RequireUniqueEmail = true }
        });

        return new UserManager<ApplicationUser>(
            store,
            options,
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()],
            [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private static RoleManager<IdentityRole<Guid>> CreateRoleManager(ApplicationDbContext dbContext)
    {
        var store = new RoleStore<IdentityRole<Guid>, ApplicationDbContext, Guid>(dbContext);
        return new RoleManager<IdentityRole<Guid>>(
            store,
            [new RoleValidator<IdentityRole<Guid>>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole<Guid>>>.Instance);
    }
}
