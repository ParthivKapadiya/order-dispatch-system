using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ToplandERP.Application.Abstractions;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.Infrastructure.Options;
using ToplandERP.Infrastructure.Storage;

namespace ToplandERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        if (!environment.IsEnvironment("Testing"))
        {
            AddDatabaseProvider(services, configuration, environment);
        }

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database");

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        return services;
    }

    private static void AddDatabaseProvider(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var configuredProvider = configuration["Database:Provider"];
        var provider = string.IsNullOrWhiteSpace(configuredProvider)
            ? (environment.IsDevelopment() ? "Sqlite" : "SqlServer")
            : configuredProvider;

        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var configuredPath = configuration.GetConnectionString("Sqlite") ?? "App_Data/toplanderp.db";
            var databasePath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));

            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));
            return;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
}
