using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ToplandERP.Infrastructure.Data;

namespace ToplandERP.IntegrationTests;

public sealed class ToplandWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Seed:SuperAdmin:UserName", "admin");
        builder.UseSetting("Seed:SuperAdmin:Email", "admin@localhost");
        builder.UseSetting("Seed:SuperAdmin:Password", "DevP@ssw0rd!123");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=ToplandERP_Unused;Trusted_Connection=True;");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("ToplandERP_IntegrationTests"));
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
}
