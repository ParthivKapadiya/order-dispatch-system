using FluentAssertions;
using FluentValidation.TestHelper;
using ToplandERP.Application.Customers;
using ToplandERP.Application.Users;

namespace ToplandERP.UnitTests.Application;

public class ValidationTests
{
    [Fact]
    public void Customer_requires_name_mobile_and_both_addresses()
    {
        var validator = new CustomerWriteRequestValidator();
        var result = validator.TestValidate(new CustomerWriteRequest());
        result.ShouldHaveValidationErrorFor(request => request.CustomerName);
        result.ShouldHaveValidationErrorFor(request => request.Mobile);
        result.ShouldHaveValidationErrorFor(request => request.BillingAddress);
        result.ShouldHaveValidationErrorFor(request => request.DeliveryAddress);
    }

    [Fact]
    public void Customer_rejects_invalid_mobile()
    {
        var validator = new CustomerWriteRequestValidator();
        var result = validator.TestValidate(new CustomerWriteRequest
        {
            CustomerCode = "C1",
            CustomerName = "Name",
            Mobile = "12345",
            BillingAddress = "A",
            BillingCity = "Ahmedabad",
            BillingState = "Gujarat",
            BillingPincode = "380001",
            DeliveryAddress = "B",
            DeliveryCity = "Rajkot",
            DeliveryState = "Gujarat",
            DeliveryPincode = "360001"
        });
        result.ShouldHaveValidationErrorFor(request => request.Mobile);
    }

    [Fact]
    public void User_requires_strong_password()
    {
        var validator = new CreateUserRequestValidator();
        var result = validator.TestValidate(new CreateUserRequest
        {
            FullName = "Patel",
            EmployeeCode = "E1",
            UserName = "patel",
            Email = "patel@example.com",
            Mobile = "9876543210",
            Role = "SalesEmployee",
            TemporaryPassword = "password"
        });
        result.ShouldHaveValidationErrorFor(request => request.TemporaryPassword);
    }

    [Fact]
    public void User_accepts_a_strong_temporary_password()
    {
        var validator = new CreateUserRequestValidator();
        var result = validator.TestValidate(new CreateUserRequest
        {
            FullName = "Patel",
            EmployeeCode = "E1",
            UserName = "patel",
            Email = "patel@example.com",
            Mobile = "9876543210",
            Role = "SalesEmployee",
            TemporaryPassword = "Sales@1234"
        });
        result.ShouldNotHaveValidationErrorFor(request => request.TemporaryPassword);
    }
}
