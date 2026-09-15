using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ToplandERP.Domain.Constants;
using ToplandERP.Infrastructure.Data;

namespace ToplandERP.IntegrationTests;

public class FoundationTests : IClassFixture<ToplandWebApplicationFactory>, IAsyncLifetime
{
    private readonly ToplandWebApplicationFactory _factory;

    public FoundationTests(ToplandWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_endpoint_returns_success()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Home_requires_authentication()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.PathAndQuery.Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task Login_page_is_available_anonymously()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, html);
        html.Should().Contain("Sign in");
    }

    [Fact]
    public void Database_context_initializes_and_seeds_companies_and_roles()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Companies.Should().HaveCount(3);
        dbContext.Roles.Select(role => role.Name).Should().BeEquivalentTo(RoleNames.All);
        dbContext.Users.Should().Contain(user => user.UserName == "admin");
    }
}
