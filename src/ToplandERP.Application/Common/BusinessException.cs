namespace ToplandERP.Application.Common;

public class BusinessException : Exception
{
    public BusinessException(string message)
        : base(message)
    {
    }
}

public sealed class NotFoundException : BusinessException
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class ForbiddenException : BusinessException
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
