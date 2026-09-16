using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;

namespace ToplandERP.IntegrationTests;

public class MasterDataAuthorizationTests : IClassFixture<ToplandWebApplicationFactory>, IAsyncLifetime
{
    private readonly ToplandWebApplicationFactory _factory;
    private Guid _jeekoCustomerId;

    public MasterDataAuthorizationTests(ToplandWebApplicationFactory factory)
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
    public async Task Anonymous_user_is_redirected_from_customers()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Customers");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.PathAndQuery.Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task CompanyAdmin_can_open_customers()
    {
        var client = await SignInAsync("gravis.admin", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Customers");
        var html = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, html);
        html.Should().Contain("Customers");
    }

    [Fact]
    public async Task Cross_company_customer_access_is_rejected()
    {
        var client = await SignInAsync("gravis.admin", "DevP@ssw0rd!123");
        var response = await client.GetAsync($"/Customers/Details/{_jeekoCustomerId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SalesEmployee_cannot_access_user_management()
    {
        var client = await SignInAsync("gravis.sales", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Users");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            response.Headers.Location!.PathAndQuery.Should().Contain("/Account/AccessDenied");
        }
    }

    [Fact]
    public async Task Deactivated_user_cannot_log_in()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "inactive.user",
            ["Password"] = "DevP@ssw0rd!123",
            ["__RequestVerificationToken"] = token
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Invalid username or password.");
    }

    [Fact]
    public async Task SuperAdmin_can_open_employees_across_companies()
    {
        var client = await SignInAsync("admin", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Users");
        var html = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, html);
        html.Should().Contain("Employees");
    }

    [Fact]
    public async Task Anonymous_user_is_redirected_from_orders()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Orders");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.PathAndQuery.Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task SalesEmployee_can_open_create_order()
    {
        var client = await SignInAsync("gravis.sales", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Orders/Create");
        var html = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, html);
        html.Should().Contain("Create order");
        html.Should().Contain("Order Received");
    }

    [Fact]
    public async Task DispatchUser_cannot_create_orders()
    {
        var client = await SignInAsync("gravis.dispatch", "DevP@ssw0rd!123");
        var response = await client.GetAsync("/Orders/Create");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            response.Headers.Location!.PathAndQuery.Should().Contain("/Account/AccessDenied");
        }
    }

    private async Task SeedOperationalUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await CreateUserAsync(users, "gravis.admin", "gravis.admin@example.com", "Gravis Admin", "GRA-ADM", SeedIdentifiers.GravisCompanyId, RoleNames.CompanyAdmin);
        await CreateUserAsync(users, "gravis.sales", "gravis.sales@example.com", "Gravis Sales", "GRA-SAL", SeedIdentifiers.GravisCompanyId, RoleNames.SalesEmployee);
        await CreateUserAsync(users, "gravis.dispatch", "gravis.dispatch@example.com", "Gravis Dispatch", "GRA-DIS", SeedIdentifiers.GravisCompanyId, RoleNames.DispatchUser);
        var inactive = await CreateUserAsync(users, "inactive.user", "inactive.user@example.com", "Inactive User", "GRA-INA", SeedIdentifiers.GravisCompanyId, RoleNames.SalesEmployee);
        inactive.IsActive = false;
        await users.UpdateAsync(inactive);

        if (!dbContext.Customers.IgnoreQueryFilters().Any(item => item.CustomerCode == "JEE-IT-001"))
        {
            var customer = new Customer
            {
                CompanyId = SeedIdentifiers.JeekoCompanyId,
                CustomerCode = "JEE-IT-001",
                CustomerName = "Integration Jeeko Customer",
                Mobile = "9876599999",
                BillingAddress = "Jeeko billing",
                BillingCity = "Rajkot",
                BillingState = "Gujarat",
                BillingPincode = "360001",
                DeliveryAddress = "Jeeko delivery",
                DeliveryCity = "Rajkot",
                DeliveryState = "Gujarat",
                DeliveryPincode = "360001",
                IsActive = true
            };
            dbContext.Customers.Add(customer);
            await dbContext.SaveChangesAsync();
            _jeekoCustomerId = customer.Id;
        }
        else
        {
            _jeekoCustomerId = dbContext.Customers.IgnoreQueryFilters().First(item => item.CustomerCode == "JEE-IT-001").Id;
        }
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
            PhoneNumber = "9876543210",
            IsActive = true
        };

        var result = await users.CreateAsync(user, "DevP@ssw0rd!123");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        await users.AddToRoleAsync(user, role);
        return user;
    }

    private async Task<HttpClient> SignInAsync(string userName, string password)
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = userName,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.OK);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var html = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed for {userName}: {html}");
        }

        return client;
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!match.Success)
        {
            match = Regex.Match(html, "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");
        }

        match.Success.Should().BeTrue("the login form should include an anti-forgery token");
        return match.Groups[1].Value;
    }
}
