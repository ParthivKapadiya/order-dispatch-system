using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ToplandERP.Domain.Constants;
using ToplandERP.Infrastructure.Identity;

namespace ToplandERP.IntegrationTests;

public class Phase3AuthorizationTests : IClassFixture<ToplandWebApplicationFactory>, IAsyncLifetime
{
    private readonly ToplandWebApplicationFactory _factory;

    public Phase3AuthorizationTests(ToplandWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.SeedAsync();
        await SeedOperationalUsersAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Anonymous_user_is_redirected_from_dispatch()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Dispatch");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.PathAndQuery.Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task SalesEmployee_cannot_open_dispatch_dashboard()
    {
        var client = await SignInAsync("gravis.sales", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Dispatch");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task DispatchUser_can_open_dispatch_dashboard()
    {
        var client = await SignInAsync("gravis.dispatch", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Dispatch");
        var html = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, html);
        html.Should().Contain("Ready to Dispatch");
    }

    [Fact]
    public async Task SalesEmployee_cannot_open_modification_queue()
    {
        var client = await SignInAsync("gravis.sales", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/ModificationRequests");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task CompanyAdmin_can_open_modification_queue()
    {
        var client = await SignInAsync("gravis.admin", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/ModificationRequests");
        var html = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, html);
        html.Should().Contain("Modification requests");
    }

    [Fact]
    public async Task DispatchUser_cannot_open_modification_queue()
    {
        var client = await SignInAsync("gravis.dispatch", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/ModificationRequests");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    private async Task SeedOperationalUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "gravis.admin", "gravis.admin@example.com", "Gravis Admin", "GRA-ADM", SeedIdentifiers.GravisCompanyId, RoleNames.CompanyAdmin);
        await CreateUserAsync(users, "gravis.sales", "gravis.sales@example.com", "Gravis Sales", "GRA-SAL", SeedIdentifiers.GravisCompanyId, RoleNames.SalesEmployee);
        await CreateUserAsync(users, "gravis.dispatch", "gravis.dispatch@example.com", "Gravis Dispatch", "GRA-DIS", SeedIdentifiers.GravisCompanyId, RoleNames.DispatchUser);
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> users,
        string userName,
        string email,
        string fullName,
        string employeeCode,
        Guid companyId,
        string role)
    {
        if (await users.FindByNameAsync(userName) is not null)
        {
            return;
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

        match.Success.Should().BeTrue("the login page should include an anti-forgery token");
        return match.Groups[1].Value;
    }
}
