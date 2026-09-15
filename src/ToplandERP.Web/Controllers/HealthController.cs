using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
public class HealthController : Controller
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "System health";
        ViewData["Breadcrumb"] = "Health";
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
        return View(report);
    }
}
