namespace OrderProcessing.Core.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }

    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}
