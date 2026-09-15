using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Web.ViewModels;

namespace ToplandERP.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("Error")]
    [Route("Error/{statusCode:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(int? statusCode)
    {
        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode
        };

        if (statusCode == StatusCodes.Status404NotFound)
        {
            model.Title = "Page not found";
            model.Message = "The page you requested does not exist or is not available yet.";
        }
        else if (statusCode == StatusCodes.Status403Forbidden)
        {
            model.Title = "Access denied";
            model.Message = "You do not have permission to view this page.";
        }

        Response.StatusCode = statusCode is > 0 ? statusCode.Value : StatusCodes.Status500InternalServerError;
        return View(model);
    }
}
