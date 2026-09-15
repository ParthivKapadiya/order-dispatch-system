namespace ToplandERP.Web.ViewModels;

public sealed class ErrorViewModel
{
    public string? RequestId { get; set; }

    public int? StatusCode { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string Title { get; set; } = "Something went wrong";

    public string Message { get; set; } = "An unexpected error occurred. Please try again or contact your administrator.";
}
