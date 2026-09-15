using ToplandERP.Infrastructure;

namespace ToplandERP.Web.Middleware;

public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await next();
        });
    }
}

public static class WebApplicationExtensions
{
    public static WebApplication UseWebPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            app.UseDeveloperExceptionPage();
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/Error/{0}");
        app.UseSecurityHeaders();
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHttpsRedirection();
        }
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health").AllowAnonymous();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        try
        {
            await app.Services.InitializeDatabaseAsync(app.Lifetime.ApplicationStopping);
        }
        catch (Exception exception)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");
            logger.LogError(exception, "Database migration or seed failed. The application cannot start until the database is available.");
            throw;
        }
    }
}
