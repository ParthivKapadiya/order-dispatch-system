using Microsoft.Extensions.DependencyInjection;
using ToplandERP.Infrastructure.Data;

namespace ToplandERP.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
