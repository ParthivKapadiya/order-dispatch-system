namespace ToplandERP.Domain.Constants;

public static class ValidationPatterns
{
    public const string Mobile = @"^(\+91[\-\s]?)?[6-9]\d{9}$";
    public const string Pincode = @"^[1-9][0-9]{5}$";
}
