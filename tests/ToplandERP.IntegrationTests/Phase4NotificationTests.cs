using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Domain.Enums;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;

namespace ToplandERP.IntegrationTests;

public class Phase4NotificationTests : IClassFixture<ToplandWebApplicationFactory>, IAsyncLifetime
{
    private readonly ToplandWebApplicationFactory _factory;
    private Guid _salesNotificationId;
    private Guid _adminNotificationId;
    private Guid _jeekoNotificationId;

    public Phase4NotificationTests(ToplandWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.SeedAsync();
        await SeedUsersAndNotificationsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Anonymous_user_is_redirected_from_notifications()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Notifications");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.PathAndQuery.Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task SalesEmployee_can_open_own_notification_center()
    {
        var client = await SignInAsync("gravis.sales", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Notifications");
        var html = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, html);
        html.Should().Contain("Sales only notice");
        html.Should().NotContain("Admin only notice");
    }

    [Fact]
    public async Task SalesEmployee_cannot_open_another_users_notification()
    {
        var client = await SignInAsync("gravis.sales", "DevP@ssw0rd!123");
        var response = await client.GetAsync($"/Notifications/Details/{_adminNotificationId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DispatchUser_cannot_open_company_admin_notification()
    {
        var client = await SignInAsync("gravis.dispatch", "DevP@ssw0rd!123");
        var response = await client.GetAsync($"/Notifications/Details/{_adminNotificationId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompanyAdmin_cannot_open_other_company_notification()
    {
        var client = await SignInAsync("gravis.admin", "DevP@ssw0rd!123");
        var response = await client.GetAsync($"/Notifications/Details/{_jeekoNotificationId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Mark_as_read_and_mark_all_as_read_are_recipient_scoped()
    {
        var salesClient = await SignInAsync("gravis.sales", "DevP@ssw0rd!123");
        var page = await salesClient.GetAsync("/Notifications");
        var html = await page.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var markOther = await salesClient.PostAsync(
            $"/Notifications/MarkAsRead/{_adminNotificationId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        markOther.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var markAll = await salesClient.PostAsync(
            "/Notifications/MarkAllAsRead",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
        markAll.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sales = await dbContext.Notifications.IgnoreQueryFilters()
            .SingleAsync(item => item.Id == _salesNotificationId);
        var admin = await dbContext.Notifications.IgnoreQueryFilters()
            .SingleAsync(item => item.Id == _adminNotificationId);
        sales.IsRead.Should().BeTrue();
        admin.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Unread_count_endpoint_returns_json_for_authenticated_user()
    {
        var client = await SignInAsync("gravis.admin", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Notifications/UnreadCount");
        var payload = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, payload);
        payload.Should().Contain("count");
    }

    [Fact]
    public async Task Mark_all_as_read_rejects_get()
    {
        var client = await SignInAsync("gravis.sales", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Notifications/MarkAllAsRead");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed, HttpStatusCode.Redirect);
    }

    private async Task SeedUsersAndNotificationsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sales = await CreateUserAsync(users, "gravis.sales", "gravis.sales@example.com", "Gravis Sales", "GRA-SAL", SeedIdentifiers.GravisCompanyId, RoleNames.SalesEmployee);
        var admin = await CreateUserAsync(users, "gravis.admin", "gravis.admin@example.com", "Gravis Admin", "GRA-ADM", SeedIdentifiers.GravisCompanyId, RoleNames.CompanyAdmin);
        await CreateUserAsync(users, "gravis.dispatch", "gravis.dispatch@example.com", "Gravis Dispatch", "GRA-DIS", SeedIdentifiers.GravisCompanyId, RoleNames.DispatchUser);
        var jeekoAdmin = await CreateUserAsync(users, "jeeko.admin", "jeeko.admin@example.com", "Jeeko Admin", "JEE-ADM", SeedIdentifiers.JeekoCompanyId, RoleNames.CompanyAdmin);

        _salesNotificationId = Guid.NewGuid();
        _adminNotificationId = Guid.NewGuid();
        _jeekoNotificationId = Guid.NewGuid();

        dbContext.Notifications.AddRange(
            new Notification
            {
                Id = _salesNotificationId,
                CompanyId = SeedIdentifiers.GravisCompanyId,
                RecipientUserId = sales.Id,
                Type = NotificationType.OrderDispatched,
                Title = "Sales only notice",
                Message = "Order GRAVIS-20260915-0001 has been dispatched.",
                RelatedEntityType = NotificationRelatedEntities.Order,
                EventKey = $"sales-{_salesNotificationId:N}"
            },
            new Notification
            {
                Id = _adminNotificationId,
                CompanyId = SeedIdentifiers.GravisCompanyId,
                RecipientUserId = admin.Id,
                Type = NotificationType.OrderCreated,
                Title = "Admin only notice",
                Message = "Order GRAVIS-20260915-0001 has been created.",
                RelatedEntityType = NotificationRelatedEntities.Order,
                EventKey = $"admin-{_adminNotificationId:N}"
            },
            new Notification
            {
                Id = _jeekoNotificationId,
                CompanyId = SeedIdentifiers.JeekoCompanyId,
                RecipientUserId = jeekoAdmin.Id,
                Type = NotificationType.OrderCreated,
                Title = "Jeeko only notice",
                Message = "Order JEEKO-20260915-0001 has been created.",
                RelatedEntityType = NotificationRelatedEntities.Order,
                EventKey = $"jeeko-{_jeekoNotificationId:N}"
            });
        await dbContext.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> users,
        string userName,
        string email,
        string fullName,
        string employeeCode,
        Guid companyId,
        string role)
    {
        var existing = await users.FindByNameAsync(userName);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            EmployeeCode = employeeCode,
            CompanyId = companyId,
            IsActive = true
        };
        (await users.CreateAsync(user, "DevP@ssw0rd!123")).Succeeded.Should().BeTrue();
        (await users.AddToRoleAsync(user, role)).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task<HttpClient> SignInAsync(string userName, string password)
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = userName,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return client;
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(html, """name="__RequestVerificationToken" type="hidden" value="([^"]+)" """);
        if (!match.Success)
        {
            match = Regex.Match(html, """value="([^"]+)" name="__RequestVerificationToken" """);
        }

        match.Success.Should().BeTrue("the page should include an anti-forgery token");
        return match.Groups[1].Value;
    }
}
